using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using NUnit.Framework;
using Revit.TestRunner.Shared.Dto;
// ReSharper disable TooWideLocalVariableScope

namespace Revit.TestRunner.Runner.Direct
{
    /// <summary>
    /// This Runner runs the corresponding Test using Reflection.
    /// </summary>
    public class ReflectionRunner
    {
        /// <summary>
        /// Execute Test described in <paramref name="test"/>.
        /// Returns a new <see cref="TestCase"/> object with the test result.
        /// </summary>
        internal async Task<TestCase> RunTest( TestCase test, UIApplication uiApplication )
        {
            TestCase result = new TestCase {
                Id = test.Id,
                AssemblyPath = test.AssemblyPath,
                TestClass = test.MethodName,
                MethodName = test.MethodName,
                State = TestState.Unknown
            };

            if( string.IsNullOrEmpty( test.Id ) ) result.Message = "Missing ID";
            if( string.IsNullOrEmpty( test.AssemblyPath ) ) result.Message = "Missing AssemblyPath";
            if( string.IsNullOrEmpty( test.TestClass ) ) result.Message = "Missing ClassName";
            if( string.IsNullOrEmpty( test.MethodName ) ) result.Message = "Missing MethodName";
            if( test.State != TestState.Unknown ) result.Message = $"Wrong not in State '{TestState.Unknown}'";

            if( !string.IsNullOrEmpty( test.Message ) ) {
                test.State = TestState.Failed;
                return result;
            }


            var possibleParams = new object[] { uiApplication, uiApplication.Application };

            object obj = null;
            MethodInfo setUp = null;
            MethodInfo tearDown = null;
            MethodInfo testMethod = null;

            try {
                Assembly assembly = Assembly.LoadFile( test.AssemblyPath );
                Type type = assembly.GetType( test.TestClass );
                obj = Activator.CreateInstance( type );

                setUp = GetMethodByAttribute( type, typeof( SetUpAttribute ) );
                testMethod = type.GetMethod( test.MethodName );
                tearDown = GetMethodByAttribute( type, typeof( TearDownAttribute ) );

                var customAttributes = testMethod.CustomAttributes;
                var extendedParams = possibleParams.ToList();

                foreach( CustomAttributeData customAttribute in customAttributes ) {
                    extendedParams.AddRange( customAttribute.ConstructorArguments.Select( a => a.Value ) );
                }

                await Invoke( obj, setUp, possibleParams );
                await Invoke( obj, testMethod, extendedParams.ToArray() );

                result.State = TestState.Passed;
            }
            catch( Exception e ) {
                ReportException( result, e );
            }
            finally {
                try {
                    await Invoke( obj, tearDown, possibleParams );
                }
                catch( Exception e ) {
                    ReportException( result, e );
                }
            }

            Log.Info( $" >> {result.TestClass}.{result.MethodName} - {result.State} - {result.Message}" );

            return result;
        }

        private async Task Invoke( object obj, MethodInfo method, object[] possibleParams )
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

        private void ReportException( TestCase @case, Exception e )
        {
            @case.State = TestState.Failed;

            Exception toLogEx = e.InnerException ?? e;

            Log.Error( toLogEx );
            @case.Message = toLogEx.Message;
            @case.StackTrace = toLogEx.StackTrace;
        }

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

        private MethodInfo GetMethodByAttribute( Type type, Type attributeType )
        {
            var listOfMethods = new List<MethodInfo>();

            foreach( MethodInfo method in type.GetMethods() ) {
                if( MarkedByAttribute( method, attributeType ) ) {
                    listOfMethods.Add( method );
                }
            }

            if( listOfMethods.Count > 1 ) throw new InvalidOperationException( $"More than method marked with '{attributeType.Name}' attribute found!" );

            return listOfMethods.SingleOrDefault();
        }

        private bool MarkedByAttribute( MethodInfo methodInfo, Type attributeType )
        {
            return methodInfo.GetCustomAttributes( true ).Select( a => a.ToString() ).Contains( attributeType.FullName );
        }
    }
}
