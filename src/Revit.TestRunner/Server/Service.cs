using System;
using System.IO;
using System.Linq;
using System.Threading;
using Autodesk.Revit.UI;
using Revit.TestRunner.Runner;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Server
{
    /// <summary>
    /// Service for handling request from the clients (explore test assembly, run tests)
    /// The service is hooked to the OnIdle event of Revit.
    /// The heartbeat information is written to the watch directory.
    /// </summary>
    public class Service
    {
        #region Members

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
        /// Initialize the watch directory. Remaining status information will be removed.
        /// </summary>
        private void Initialize()
        {
            if( !Directory.Exists( FileNames.WatchDirectory ) ) {
                Directory.CreateDirectory( FileNames.WatchDirectory );
            }

            if( File.Exists( FileNames.RunnerStatusFilePath ) ) {
                FileHelper.DeleteWithLock( FileNames.RunnerStatusFilePath );
            }
        }
        #endregion

        #region Execute

        /// <summary>
        /// Main Service method. Observe the watch directory for new requests.
        /// Only one request will be processed. Write status file on every call.
        /// </summary>
        private void OnIdle( object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e )
        {
            var process = GetRequest<ExploreRequest>( FileNames.ExploreRequestBase );
            if( process.Request == null ) process = GetRequest<RunRequest>( FileNames.RunRequestBase );

            if( process.Request != null ) {
                switch( process.Request ) {
                    case ExploreRequest exploreRequest: Explore( exploreRequest, process.Response ); break;
                    case RunRequest runRequest: Run( runRequest, process.Response, sender as UIApplication ); break;
                    default: Thread.Sleep( 250 ); break;
                }
            }

            SetStatus();
        }

        /// <summary>
        /// Write the status of the service to the watch directory.
        /// </summary>
        private void SetStatus()
        {
            RunnerStatus status = new RunnerStatus {
                Timestamp = DateTime.Now,
                LogFilePath = Log.LogFilePath,
                RevitVersion = mApplication?.ControlledApplication?.VersionName
            };

            JsonHelper.ToFile( FileNames.RunnerStatusFilePath, status );
        }

        /// <summary>
        /// Explore a test assembly. 
        /// </summary>
        private void Explore( ExploreRequest request, Response response )
        {
            if( request == null ) throw new ArgumentNullException( nameof( request ) );
            if( response == null ) throw new ArgumentNullException( nameof( response ) );

            NUnitRunner runner = new NUnitRunner( request.AssemblyPath, response.Directory );
            string message = runner.ExploreAssembly();

            runner.Dispose();

            ExploreResponse exploreResponse = new ExploreResponse {
                Timestamp = DateTime.Now,
                Id = request.Id,
                AssemblyPath = request.AssemblyPath,
                ExploreFile = runner.ExploreResultFile,
                Message = message
            };

            JsonHelper.ToFile( Path.Combine( response.Directory, FileNames.ExploreResponseFileName ), exploreResponse );
        }

        /// <summary>
        /// Run desired tests.
        /// </summary>
        private void Run( RunRequest request, Response response, UIApplication application )
        {
            if( request == null ) throw new ArgumentNullException( nameof( request ) );
            if( response == null ) throw new ArgumentNullException( nameof( response ) );

            string summaryPath = Path.Combine( response.Directory, FileNames.RunSummary );
            LogInfo( summaryPath, $"Test Request '{request.Id}' - {request.ClientName} ({request.ClientVersion})" );

            RunResult runResult = new RunResult {
                Id = request.Id,
                StartTime = DateTime.Now,
                State = TestState.Running,
                Cases = request.Cases.ToArray(),
                SummaryFile = summaryPath
            };

            RevitTask runnerTask = new RevitTask();
            ReflectionRunner runner = new ReflectionRunner();

            runnerTask.Run( async uiApplication => {
                try {
                    runResult.Cases = request.Cases;

                    foreach( TestCase test in request.Cases ) {
                        var runTestResult = runResult.Cases.Single( t => t.Id == test.Id );

                        WriteTestResultFile( response.Directory, runResult, false );

                        var testResult = await runner.RunTest( test, application );

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

                WriteTestResultFile( response.Directory, runResult, true );

                LogInfo( summaryPath, $"Test run end - duration {runResult.Timestamp - runResult.StartTime}" );

            } );
        }

        /// <summary>
        /// Write test result file to response directory.
        /// </summary>
        private void WriteTestResultFile( string runDirectory, RunResult result, bool finished )
        {
            if( finished ) {
                result.State = result.Cases.Any( t => t.State == TestState.Failed ) ? TestState.Failed : TestState.Passed;
            }
            else {
                result.State = TestState.Running;
            }

            result.Timestamp = DateTime.Now;

            JsonHelper.ToFile( Path.Combine( runDirectory, FileNames.RunResult ), result );
        }


        /// <summary>
        /// Get the next request from watch directory.
        /// </summary>
        private (BaseRequest Request, Response Response) GetRequest<TRequest>( string baseFileName ) where TRequest : BaseRequest
        {
            BaseRequest request = null;
            Response response = null;

            var files = Directory.GetFiles( FileNames.WatchDirectory, $"{baseFileName}*.json" );
            var file = files.FirstOrDefault();

            if( file != null ) {
                try {
                    request = JsonHelper.FromFile<TRequest>( file );

                    if( request == null ) throw new NullReferenceException( nameof(request) );

                    var directory = FileHelper.GetDirectory( Path.Combine( FileNames.WatchDirectory, request.Id ) );
                    directory.Create();

                    response = new Response {
                        Timestamp = DateTime.Now,
                        Id = request.Id,
                        Directory = directory.FullName
                    };

                    JsonHelper.ToFile( FileNames.ResponseFilePath( request.Id ), response );
                    FileHelper.MoveWithLock( file, Path.Combine( directory.FullName, FileNames.RunRequest ) );
                }
                catch( Exception ) {
                    FileHelper.DeleteWithLock( file );
                }
            }

            return (request, response);
        }

        private void LogInfo( string summaryPath, object message )
        {
            Log.Info( message );
            File.AppendAllText( summaryPath, message + "\n" );
        }
        #endregion
    }
}
