using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using NUnit.Framework;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Runner
{
    /// <summary>
    /// This Runner runs the corresponding Test using Reflection.
    /// </summary>
    public class ReflectionRunner
    {
        private readonly Dictionary<Type, object> mTestInstances = new Dictionary<Type, object>();

        /// <summary>
        /// Execute Test described in <paramref name="test"/>.
        /// Returns a new <see cref="TestCaseDto"/> object with the test result.
        /// </summary>
        internal async Task RunTest( TestCaseDto test, bool isSingleTest, UIApplication uiApplication, Action updateAction )
        {
            Log.Info( $"Test Case {test.TestClass}.{test.MethodName}" );

            if( string.IsNullOrEmpty( test.Id ) ) test.Message = "Missing ID";
            if( string.IsNullOrEmpty( test.AssemblyPath ) ) test.Message = "Missing AssemblyPath";
            if( string.IsNullOrEmpty( test.TestClass ) ) test.Message = "Missing ClassName";
            if( string.IsNullOrEmpty( test.MethodName ) ) test.Message = "Missing MethodName";
            if( test.State != TestState.Unknown && test.State != TestState.Running ) test.Message = $"Test not in State '{TestState.Unknown}'";

            if( !string.IsNullOrEmpty( test.Message ) ) {
                test.State = TestState.Failed;
                Log.Error( $"> {test.State} - {test.Message}" );
                return;
            }

            var possibleParams = new object[] { uiApplication, uiApplication.Application };

            object testInstance = null;
            MethodInfo setUp = null;
            MethodInfo tearDown = null;
            MethodInfo testMethod = null;

            test.StartTime = DateTime.Now;
            test.State = TestState.Running;

            updateAction();

            try {
                if( !File.Exists( test.AssemblyPath ) ) throw new FileNotFoundException( $"Assembly not found! {test.AssemblyPath}" );

                Assembly assembly = Assembly.LoadFile( test.AssemblyPath );
                Type type = assembly.GetType( test.TestClass );

                if( type == null ) throw new ArgumentException( $"Type not found! {test.TestClass}" );

                if( mTestInstances.ContainsKey( type ) ) {
                    testInstance = mTestInstances[type];
                }
                else {
                    testInstance = Activator.CreateInstance( type );
                    mTestInstances.Add( type, testInstance );
                }

                setUp = GetMethodByAttribute( type, typeof( SetUpAttribute ) );
                testMethod = type.GetMethod( test.MethodName );
                tearDown = GetMethodByAttribute( type, typeof( TearDownAttribute ) );

                if( testMethod == null ) throw new ArgumentException( $"Method not found! {test.MethodName}" );

                var customAttributes = testMethod.CustomAttributes;
                var extendedParams = possibleParams.ToList();

                foreach( CustomAttributeData customAttribute in customAttributes ) {
                    extendedParams.AddRange( customAttribute.ConstructorArguments.Select( a => a.Value ) );
                }

                if( !isSingleTest && MarkedByAttribute( testMethod, typeof( ExplicitAttribute ) ) ) {
                    test.State = TestState.Explicit;
                    test.Message = "Test is marked as Explicit";
                }
                else if( MarkedByAttribute( testMethod, typeof( IgnoreAttribute ) ) ) {
                    test.State = TestState.Ignore;
                    test.Message = "Test is marked as Ignore";
                }
                else {
                    await InvokeMethod( testInstance, setUp, possibleParams );
                    await InvokeMethod( testInstance, testMethod, extendedParams.ToArray() );

                    test.State = TestState.Passed;
                }

                Log.Info( $"> {test.State} - {test.Message}" );
            }
            catch( Exception e ) {
                ReportException( test, e );
            }
            finally {
                try {
                    if( test.State == TestState.Passed || test.State == TestState.Failed ) {
                        await InvokeMethod( testInstance, tearDown, possibleParams );
                    }
                }
                catch( Exception e ) {
                    ReportException( test, e );
                }
            }

            test.EndTime = DateTime.Now;

            updateAction();
        }

        /// <summary>
        /// Invoke <paramref name="method"/> on <paramref name="obj"/>, passing <paramref name="possibleParams"/>.
        /// </summary>
        private async Task InvokeMethod( object obj, MethodInfo method, object[] possibleParams )
        {
            if( method != null ) {
                var methodParams = OrderParameters( method, possibleParams );

                if( method.ReturnType == typeof( Task ) ) {
                    Task task = (Task)method.Invoke( obj, methodParams );
                    await task;
                }
                else {
                    method.Invoke( obj, methodParams );
                }
            }
        }

        /// <summary>
        /// Enrich test case with exception information.
        /// </summary>
        private void ReportException( TestCaseDto @case, Exception e )
        {
            @case.State = TestState.Failed;

            Exception toLogEx = e.InnerException ?? e;

            Log.Error( $"> {@case.State} - {e.Message}", toLogEx );
            @case.Message = toLogEx.Message;
            @case.StackTrace = toLogEx.StackTrace;
        }

        /// <summary>
        /// Order parameters according to the method info.
        /// </summary>
        private object[] OrderParameters( MethodInfo methodInfo, object[] possibleParams )
        {
            var result = new List<object>();
            var parameters = methodInfo.GetParameters();
            var possibleParamsList = possibleParams.ToList();

            foreach( ParameterInfo parameter in parameters ) {
                object o = possibleParamsList.FirstOrDefault( i => i.GetType() == parameter.ParameterType );
                possibleParamsList.Remove( o );
                result.Add( o );
            }

            return result.ToArray();
        }

        /// <summary>
        /// Get method of <paramref name="type"/> marked by attribute.
        /// Only 1 methode marked with specific attribute is allowed.
        /// </summary>
        private MethodInfo GetMethodByAttribute( Type type, Type attributeType )
        {
            var listOfMethods = new List<MethodInfo>();

            foreach( MethodInfo method in type.GetMethods() ) {
                if( MarkedByAttribute( method, attributeType ) ) {
                    listOfMethods.Add( method );
                }
            }

            if( listOfMethods.Count > 1 ) throw new InvalidOperationException( $"More than one method marked with '{attributeType.Name}' attribute found!" );

            return listOfMethods.SingleOrDefault();
        }

        private bool MarkedByAttribute( MethodInfo methodInfo, Type attributeType )
        {
            return methodInfo.GetCustomAttributes( true ).Select( a => a.ToString() ).Contains( attributeType.FullName );
        }
    }
}
