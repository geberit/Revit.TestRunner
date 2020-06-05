using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.UI;
using NUnit.Framework;
using Revit.TestRunner.Runner.NUnit;
using Revit.TestRunner.View.TestTreeView;

namespace Revit.TestRunner.Runner.Direct
{
    /// <summary>
    /// This Runner runs the corresponding Test using Reflection.
    /// </summary>
    public class ReflectionRunner
    {
        public ReflectionRunner( string aAssemblyPath )
        {
            AssemblyPath = aAssemblyPath;
        }

        private string AssemblyPath { get; }

        internal void RunTest( NodeViewModel test, UIApplication uiApplication )
        {
            string methodName = test.MethodName;

            if( test.Parent is NodeViewModel parent ) {
                string className = parent.ClassName;

                if( !string.IsNullOrEmpty( className ) && !string.IsNullOrEmpty( methodName ) ) {
                    var possibleParams = new object[] { uiApplication, uiApplication.Application };

                    object obj = null;
                    MethodInfo setUp = null;
                    MethodInfo tearDown = null;
                    MethodInfo testMethod = null;

                    try {
                        Assembly assembly = Assembly.LoadFile( AssemblyPath );
                        Type type = assembly.GetType( className );
                        obj = Activator.CreateInstance( type );

                        setUp = GetMethodByAttribute( type, typeof( SetUpAttribute ) );
                        testMethod = type.GetMethod( methodName );
                        tearDown = GetMethodByAttribute( type, typeof( TearDownAttribute ) );

                        var customAttributes = testMethod.CustomAttributes;
                        var extendedParams = possibleParams.ToList();

                        foreach( CustomAttributeData customAttribute in customAttributes ) {
                            extendedParams.AddRange( customAttribute.ConstructorArguments.Select( a => a.Value ) );
                        }

                        Invoke( obj, setUp, possibleParams );
                        Invoke( obj, testMethod, extendedParams.ToArray() );

                        test.State = TestState.Passed;
                    }
                    catch( Exception e ) {
                        ReportException( test, e );
                    }
                    finally {
                        try {
                            Invoke( obj, tearDown, possibleParams );
                        }
                        catch( Exception e ) {
                            ReportException( test, e );
                        }
                    }

                    Log.Info( $" >> {test.FullName} - {test.State} - {test.Message}" );
                }
            }
        }

        private void Invoke( object obj, MethodInfo method, object[] possibleParams )
        {
            if( method != null ) {
                var methodParams = OrderParameters( method, possibleParams );
                method.Invoke( obj, methodParams );
            }
        }

        private void ReportException( NodeViewModel node, Exception e )
        {
            node.State = TestState.Failed;

            Exception toLogEx = e.InnerException ?? e;

            Log.Error( toLogEx );
            node.Message = toLogEx.Message;
            node.StackTrace = toLogEx.StackTrace;
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
