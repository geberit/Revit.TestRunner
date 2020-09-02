using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.UI;
using Revit.TestRunner.Runner.Direct;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Server
{
    public class Service
    {
        private const string ServiceStatus = "status.json";
        private const string RunRequestFileName = "request.json";
        private const string RunResultFileName = "result.json";

        private readonly string mWatchDirectoryPath;
        private DirectoryInfo mWatchDirectory;
        private UIControlledApplication mApplication;

        private DirectoryInfo mCurrentRunDirectory = null;

        public Service( string aWatchDirectory )
        {
            mWatchDirectoryPath = aWatchDirectory;
        }

        internal void Start( UIControlledApplication application )
        {
            mWatchDirectory = FileHelper.GetDirectory( mWatchDirectoryPath );

            mApplication = application;
            mApplication.Idling += OnIdle;

            Log.Info( $"Service started '{DateTime.Now}'" );
        }

        internal void Stop()
        {
            mApplication.Idling -= OnIdle;
            mApplication = null;

            Log.Info( $"Service stopped '{DateTime.Now}'" );
        }

        private void OnIdle( object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e )
        {
            var files = mWatchDirectory.GetFiles( "*.json" );
            var file = files.FirstOrDefault( f => f.Name != ServiceStatus );

            if( file != null ) {
                UIApplication uiApplication = sender as UIApplication;
                Run( file, uiApplication );
            }

            SetStatus();
        }

        private void SetStatus()
        {
            RunnerStatus status = new RunnerStatus();
            status.Timestamp = DateTime.Now;
            status.CurrentRun = mCurrentRunDirectory?.FullName;
            status.LogFilePath = Log.LogFilePath;

            JsonHelper.ToFile( Path.Combine( mWatchDirectoryPath, ServiceStatus ), status );
        }

        private void Run( FileInfo file, UIApplication application )
        {
            if( file != null && file.Exists ) {
                RunRequest request = null;

                try {
                    request = JsonHelper.FromFile<RunRequest>( file.FullName );
                }
                catch( Exception ) {
                    FileHelper.DeleteWithLock( file.FullName );
                }

                if( request != null ) {
                    DateTime startTime = DateTime.Now;

                    string directoryName = $"{startTime:yyyyMMdd_HHmmss}-{request.Id}";
                    mCurrentRunDirectory = FileHelper.GetDirectory( Path.Combine( mWatchDirectory.FullName, directoryName ) );

                    string summaryPath = Path.Combine( mCurrentRunDirectory.FullName, "summary.txt" );
                    LogInfo( summaryPath, $"Test Request '{request.Id}' - {request.ClientName} ({request.ClientVersion})" );

                    RunResult runResult = new RunResult();
                    runResult.Id = request.Id;
                    runResult.StartTime = startTime;
                    runResult.State = TestState.Running;
                    runResult.Cases = request.Cases.ToArray();
                    runResult.SummaryFile = summaryPath;

                    RevitTask runnerTask = new RevitTask();
                    ReflectionRunner runner = new ReflectionRunner();

                    runnerTask.Run( async uiApplication => {
                        try {
                            FileHelper.MoveWithLock( file.FullName, Path.Combine( mCurrentRunDirectory.FullName, RunRequestFileName ) );

                            runResult.Cases = request.Cases;

                            foreach( TestCase test in request.Cases ) {
                                var runTestResult = runResult.Cases.Single( t => t.Id == test.Id );

                                WriteTestStateFile( mCurrentRunDirectory, runResult, false );

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

                        WriteTestStateFile( mCurrentRunDirectory, runResult, true );

                        LogInfo( summaryPath, $"Test run end - duration {runResult.Timestamp - runResult.StartTime}" );

                    } );
                }
            }
        }

        private void LogInfo( string summarypath, object message )
        {
            Log.Info( message );
            File.AppendAllText( summarypath, message + "\n" );
        }


        private void WriteTestStateFile( DirectoryInfo runDirectory, RunResult result, bool finished )
        {
            if( finished ) {
                result.State = result.Cases.Any( t => t.State == TestState.Failed ) ? TestState.Failed : TestState.Passed;
            }
            else {
                result.State = TestState.Running;
            }

            result.Timestamp = DateTime.Now;

            JsonHelper.ToFile( Path.Combine( runDirectory.FullName, RunResultFileName ), result );
        }



    }
}
