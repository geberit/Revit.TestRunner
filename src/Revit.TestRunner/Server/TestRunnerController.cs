using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.UI;
using Revit.TestRunner.Runner;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Communication.Dto;
using Revit.TestRunner.Shared.Communication.Server;

namespace Revit.TestRunner.Server
{
    public class TestRunnerController
    {
        #region Constants, Members

        private const string ExplorePath = "explore";
        private const string TestPath = "test";
        private const string DownloadDirectory = "_download";
        private const string TestDirectory = "_test";

        private FileServer mServer;
        private UIApplication mUiApplication;
        private UIControlledApplication mApplication;

        #endregion

        #region Manage

        /// <summary>
        /// Start the service.
        /// </summary>
        internal void Start( UIControlledApplication application )
        {
            mApplication = application;
            mApplication.Idling += OnIdle;

            Initialize();

            Log.Info( $"Service started '{DateTime.Now}'" );
        }

        /// <summary>
        /// Stop the service.
        /// </summary>
        internal void Stop()
        {
            mApplication.Idling -= OnIdle;
            mApplication = null;

            Log.Info( $"Service stopped '{DateTime.Now}'" );
        }

        /// <summary>
        /// Initialize the file server. Routes are ready for requests.
        /// Processing <see cref="OnIdle"/>.
        /// </summary>
        private void Initialize()
        {
            mServer = new FileServer( FileNames.WatchDirectory );
            mServer.RegisterRoute<NoParameterDto, HomeDto>( "", ProcessHome, true );
            mServer.RegisterRoute<ExploreRequestDto, ExploreResponseDto>( ExplorePath, ProcessExplore );
            mServer.RegisterRoute<TestRequestDto, TestResponseDto>( TestPath, ProcessTests );

            mServer.StartConcurrentRoutes();
        }

        /// <summary>
        /// Main Service method. Observe the watch directory for new requests.
        /// Only one request will be processed. Write status file on every call.
        /// </summary>
        private void OnIdle( object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e )
        {
            mUiApplication = sender as UIApplication;
            mServer.ProceedNextNotConcurrent();
        }
        #endregion

        #region Process

        /// <summary>
        /// Process home request.
        /// </summary>
        private HomeDto ProcessHome( object request )
        {
            Log.Info( "Process Home request" );

            var result = new HomeDto {
                LogFilePath = Log.LogFilePath,
                RevitVersion = mApplication?.ControlledApplication?.VersionName,
                ExplorePath = Path.Combine( FileNames.WatchDirectory, ExplorePath ),
                TestPath = Path.Combine( FileNames.WatchDirectory, TestPath )
            };

            return result;
        }

        /// <summary>
        /// Process explore assembly request.
        /// </summary>
        private ExploreResponseDto ProcessExplore( ExploreRequestDto request )
        {
            Log.Info( "Process Explore request" );

            var downloadDir = Path.Combine( FileNames.WatchDirectory, DownloadDirectory, request.RequestId );
            Directory.CreateDirectory( downloadDir );

            NUnitRunner runner = new NUnitRunner( request.AssemblyPath, downloadDir );
            string message = runner.ExploreAssembly();

            var result = new ExploreResponseDto {
                AssemblyPath = request.AssemblyPath,
                ExploreFile = runner.ExploreResultFile,
                Message = message
            };

            return result;

        }

        /// <summary>
        /// Process Test execution.
        /// </summary>
        private TestResponseDto ProcessTests( TestRequestDto request )
        {
            Log.Info( "Process Test request" );

            var testDir = Path.Combine( FileNames.WatchDirectory, TestDirectory, request.RequestId );
            Directory.CreateDirectory( testDir );

            var result = new TestResponseDto {
                ResponseDirectory = testDir
            };

            string summaryPath = Path.Combine( testDir, FileNames.RunSummary );
            LogInfo( summaryPath, $"Test Request '{request.RequestId}' - {request.ClientName} ({request.ClientVersion})" );

            RunResult runResult = new RunResult {
                Id = request.RequestId,
                StartTime = DateTime.Now,
                State = TestState.Running,
                Cases = request.Cases.ToArray(),
                SummaryFile = summaryPath
            };

            RevitTask runnerTask = new RevitTask();
            ReflectionRunner runner = new ReflectionRunner();

            runnerTask.Run( async uiApplication => {
                try {
                    var casesToRun = request.Cases
                        .OrderBy( c => c.AssemblyPath )
                        .ThenBy( c => c.TestClass )
                        .ThenBy( c => c.MethodName )
                        .ToArray();

                    runResult.Cases = casesToRun;

                    foreach( TestCase test in casesToRun ) {
                        var runTestResult = runResult.Cases.Single( t => t.Id == test.Id );

                        WriteTestResultFile( testDir, runResult, false );

                        var testResult = await runner.RunTest( test, mUiApplication );

                        runTestResult.State = testResult.State;
                        runTestResult.Message = testResult.Message;
                        runTestResult.StackTrace = testResult.StackTrace;

                        LogInfo( summaryPath, $"{test.Id,-8} Test {test.State,-7} - {test.TestClass}.{test.MethodName}" );

                        if( !string.IsNullOrEmpty( test.Message ) ) LogInfo( summaryPath, $"\t{test.Message}" );
                        if( !string.IsNullOrEmpty( test.StackTrace ) ) LogInfo( summaryPath, $"\t{test.StackTrace}" );
                    }

                }
                catch( Exception e ) {
                    runResult.Output = e.ToString();
                    LogInfo( summaryPath, e );
                }

                WriteTestResultFile( testDir, runResult, true );

                LogInfo( summaryPath, $"Test run end - duration {runResult.Timestamp - runResult.StartTime}" );

            } );

            return result;
        }

        /// <summary>
        /// Write test result file to response directory.
        /// </summary>
        private void WriteTestResultFile( string directory, RunResult result, bool finished )
        {
            if( finished ) {
                result.State = result.Cases.Any( t => t.State == TestState.Failed ) ? TestState.Failed : TestState.Passed;
            }
            else {
                result.State = TestState.Running;
            }

            result.Timestamp = DateTime.Now;

            JsonHelper.ToFile( Path.Combine( directory, FileNames.RunResult ), result );
        }

        private void LogInfo( string summaryPath, object message )
        {
            Log.Info( message );
            File.AppendAllText( summaryPath, message + "\n" );
        }
        #endregion
    }
}
