using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using NUnit.Framework;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Runner
{
    /// <summary>
    /// Represents a test class instance.
    /// </summary>
    public class TestInstance
    {
        #region Members, Constructor

        private readonly string mAssemblyName;
        private readonly string mTypeName;
        private MethodInfo mOneTimeSetUp;
        private MethodInfo mOneTimeTearDown;
        private MethodInfo mSetUp;
        private MethodInfo mTearDown;
        private object[] mPossibleParams;

        /// <summary>
        /// Constructor
        /// </summary>
        public TestInstance( string assemblyPath, string typeName )
        {
            mAssemblyName = assemblyPath;
            mTypeName = typeName;

            Initialize();
            FindNUnitMethods();
        }
        #endregion

        #region Properties

        /// <summary>
        /// Get the the <see cref="System.Type"/>
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Get the test instance.
        /// </summary>
        public object Instance { get; private set; }

        /// <summary>
        /// Get true if Type is marked with <see cref="ExplicitAttribute"/>.
        /// </summary>
        public bool IsExplicitMarked { get; private set; }
        #endregion

        #region Methods

        /// <summary>
        /// Analyze the method.
        /// </summary>
        public TestState AnalyzeTestMethod( string methodName, bool isSingle )
        {
            var method = Type.GetMethod( methodName );
            if( method == null ) throw new ArgumentException( $"Method not found! {methodName}" );

            var isExplicitMarked = MarkedByAttribute( method, typeof( ExplicitAttribute ) ) ||
                                   IsExplicitMarked && !isSingle;
            var isIgnoreMarked = MarkedByAttribute( method, typeof( IgnoreAttribute ) );
            var hasTestCaseAttribute = MarkedByAttribute( method, typeof( TestCaseAttribute ) );

            if( isIgnoreMarked ) return TestState.Ignore;
            if( isExplicitMarked && !isSingle ) return TestState.Explicit;
            if( hasTestCaseAttribute ) throw new NotImplementedException( "TestCaseAttribute is not supported!" );

            return TestState.Passed;
        }

        /// <summary>
        /// Executes the test method.
        /// </summary>
        public async Task ExecuteTestMethod( string methodName )
        {
            var method = Type.GetMethod( methodName );

            try {
                await InvokeMethod( Instance, mSetUp, mPossibleParams );
                await InvokeMethod( Instance, method, mPossibleParams );
            }
            catch(TargetInvocationException e ) {
                if( e.InnerException is SuccessException se ) {
                    // ignore, test pass legally by Assert.Pass()
                    Console.WriteLine( $"Pass: {se.Message}" );
                }
                else {
                    throw;
                }
            }
            finally {
                await InvokeMethod( Instance, mTearDown, mPossibleParams );
            }
        }

        /// <summary>
        /// Create a new instance of the test class.
        /// </summary>
        public async Task CreateTestInstance( UIApplication uiApplication )
        {
            if( Instance != null ) throw new Exception( "test instance already created!" );

            mPossibleParams = new object[] { uiApplication, uiApplication.Application };

            Instance = Activator.CreateInstance( Type );

            await InvokeMethod( Instance, mOneTimeSetUp, mPossibleParams );
        }

        /// <summary>
        /// Distroy the test class instance.
        /// </summary>
        public async Task DisposeTestInstance()
        {
            if( Instance == null ) throw new Exception( "test instance not created!" );

            await InvokeMethod( Instance, mOneTimeTearDown, mPossibleParams );

            if( Instance is IDisposable disposableObject ) {
                disposableObject.Dispose();
            }

            Instance = null;
            Type = null;
            mPossibleParams = null;
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
        /// Create the Test instance.
        /// </summary>
        private void Initialize()
        {
            if( !File.Exists( mAssemblyName ) ) throw new FileNotFoundException( $"Assembly not found! {mAssemblyName}" );

            Assembly assembly = Assembly.LoadFile( mAssemblyName );
            Type = assembly.GetType( mTypeName );

            if( Type == null ) throw new ArgumentException( $"Type not found! {mTypeName}" );


        }

        /// <summary>
        /// Find all NUnit specific class methods.
        /// </summary>
        private void FindNUnitMethods()
        {
            mOneTimeSetUp = GetMethodByAttribute( Type, typeof( OneTimeSetUpAttribute ) );
            mOneTimeTearDown = GetMethodByAttribute( Type, typeof( OneTimeTearDownAttribute ) );
            mSetUp = GetMethodByAttribute( Type, typeof( SetUpAttribute ) );
            mTearDown = GetMethodByAttribute( Type, typeof( TearDownAttribute ) );

            IsExplicitMarked = MarkedByAttribute( Type.GetTypeInfo(), typeof( ExplicitAttribute ) );
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

        private bool MarkedByAttribute( TypeInfo methodInfo, Type attributeType )
        {
            return methodInfo.GetCustomAttributes( true ).Select( a => a.ToString() ).Contains( attributeType.FullName );
        }
        #endregion
    }
}
