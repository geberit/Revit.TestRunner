using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.UI;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Runner
{
    /// <summary>
    /// This Runner runs the corresponding Test using Reflection.
    /// </summary>
    public class ReflectionRunner
    {
        private readonly UIApplication mUiApplication;

        /// <summary>
        /// Constructor
        /// </summary>
        public ReflectionRunner( UIApplication uiApplication )
        {
            mUiApplication = uiApplication;
        }

        /// <summary>
        /// Execute a Group of tests, where the group key is the test class name.
        /// A single instance for the test class is created, and all tests are executed.
        /// After each test, <paramref name="updateAction"/> if called
        /// </summary>
        internal async Task RunTestClassGroup( IGrouping<string, TestCaseDto> testClassGroup, bool isSingleTest, Action<TestCaseDto, bool> updateAction )
        {
            if( !testClassGroup.Any() ) return;

            var instance = new TestInstance( testClassGroup.First().AssemblyPath, testClassGroup.Key );

            try {
                updateAction( testClassGroup.First(), false );
                await instance.CreateTestInstance( mUiApplication );
            }
            catch( Exception e ) {
                foreach( TestCaseDto test in testClassGroup ) {
                    test.Message = $"Exception in OneTimeSetup: {e.Message}";
                    updateAction( test, true );
                }

                Log.Error( $"> {testClassGroup.Key} - {e.Message}", e );
            }

            // run tests
            foreach( TestCaseDto test in testClassGroup ) {
                Log.Info( $"Test Case {test.TestClass}.{test.MethodName}" );
                
                var sw = new StringWriter();
                Console.SetOut(sw);
                Console.SetError(sw);
                string consoleString = string.Empty;

                try {
                    test.StartTime = DateTime.Now;
                    test.State = TestState.Running;

                    if( !ValidateProperties( test ) || !ValidateEnvironment( test, instance, isSingleTest ) ) {
                        Log.Error( $"> {test.State}: {test.Message}" );
                        updateAction( test, true );
                        continue;
                    }

                    updateAction( test, false );

                    await instance.ExecuteTestMethod( test.MethodName );

                    test.State = TestState.Passed;
                    consoleString = sw.ToString();

                    Log.Info( $"> {test.State}: {test.Message}" );
                }
                catch( Exception e ) {
                    test.State = TestState.Failed;
                    consoleString = sw.ToString();

                    Exception toLogEx = e.InnerException ?? e;

                    Log.Error( $"> {test.State}: {e.Message}", toLogEx );
                    test.Message = toLogEx.Message;
                    test.StackTrace = toLogEx.StackTrace;
                }
                finally {
                    test.EndTime = DateTime.Now;

                    test.Message += "\n" + consoleString;
                    test.Message = test.Message.Trim( '\n' );

                    updateAction( test, true );
                }
            }

            try {
                await instance.DisposeTestInstance();
            }
            catch( Exception e ) {
                Log.Error( $"> {testClassGroup.Key} - {e.Message}", e );
            }
        }

        /// <summary>
        /// Validate the input <paramref name="test"/> for correct environment against the instance and run.
        /// </summary>
        private bool ValidateEnvironment( TestCaseDto test, TestInstance instance, bool isSingleTest )
        {
            var state = instance.AnalyzeTestMethod( test.MethodName, isSingleTest );

            if( state == TestState.Ignore ) {
                test.State = TestState.Ignore;
                test.Message = "Test is marked as Ignore";
            }
            else if( state == TestState.Explicit ) {
                test.State = TestState.Explicit;
                test.Message = "Test is marked as Explicit";
            }

            return string.IsNullOrEmpty( test.Message );
        }

        /// <summary>
        /// Validate the input <paramref name="test"/> for correct properties.
        /// </summary>
        private bool ValidateProperties( TestCaseDto test )
        {
            if( string.IsNullOrEmpty( test.Id ) ) test.Message = "Missing ID";
            if( string.IsNullOrEmpty( test.AssemblyPath ) ) test.Message = "Missing AssemblyPath";
            if( string.IsNullOrEmpty( test.TestClass ) ) test.Message = "Missing ClassName";
            if( string.IsNullOrEmpty( test.MethodName ) ) test.Message = "Missing MethodName";
            if( test.State != TestState.Unknown && test.State != TestState.Running ) test.Message = $"Test not in State '{TestState.Unknown}'";

            if( !string.IsNullOrEmpty( test.Message ) ) {
                test.State = TestState.Failed;
            }

            return string.IsNullOrEmpty( test.Message );
        }
    }
}
