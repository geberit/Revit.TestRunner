using System;
using System.IO;
using System.Linq;
using Autodesk.Revit.UI;
using Revit.TestRunner.Runner.Direct;
using Revit.TestRunner.Runner.NUnit;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Server
{
    public class Service
    {
        private UIControlledApplication mApplication;


        internal void Start( UIControlledApplication application )
        {
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
            var exploreRequestFile = GetNextExploreRequestFile();
            var runRequestFile = GetNextRunRequestFile();

            if( !string.IsNullOrEmpty( exploreRequestFile ) ) {
                Explore( exploreRequestFile );
            }
            else if( !string.IsNullOrEmpty( runRequestFile ) ) {
                UIApplication uiApplication = sender as UIApplication;
                Run( runRequestFile, uiApplication );
            }

            SetStatus();
        }

        private void SetStatus()
        {
            RunnerStatus status = new RunnerStatus {
                Timestamp = DateTime.Now,
                LogFilePath = Log.LogFilePath
            };

            JsonHelper.ToFile( FileNames.RunnerStatusFilePath, status );
        }

        private void WriteResponse( string id, string responseDirectory )
        {
            Response response = new Response {
                Timestamp = DateTime.Now,
                Id = id,
                Directory = responseDirectory
            };

            JsonHelper.ToFile( FileNames.ResponseFilePath( id ), response );
        }

        private void Explore( string filePath )
        {
            if( File.Exists( filePath ) ) {
                ExploreRequest request = null;

                try {
                    request = JsonHelper.FromFile<ExploreRequest>( filePath );
                }
                catch {
                    FileHelper.DeleteWithLock( filePath );
                }

                if( request != null ) {
                    var responseDirectory = CreateResponseDirectory( request.Id );
                    WriteResponse( request.Id, responseDirectory.FullName );

                    FileHelper.MoveWithLock( filePath, Path.Combine( responseDirectory.FullName, FileNames.RunRequest ) );

                    NUnitRunner runner = new NUnitRunner( request.AssemblyPath );
                    (string file, string message) = runner.ExploreAssembly( responseDirectory.FullName );

                    runner.Dispose();

                    ExploreResponse response = new ExploreResponse {
                        Timestamp = DateTime.Now,
                        Id = request.Id,
                        AssemblyPath = request.AssemblyPath,
                        ExploreFile = file,
                        Message = message
                    };

                    JsonHelper.ToFile( Path.Combine( responseDirectory.FullName, FileNames.ExploreResponseFileName ), response );
                }
            }
        }

        private void Run( string filePath, UIApplication application )
        {
            if( File.Exists( filePath ) ) {
                RunRequest request = null;

                try {
                    request = JsonHelper.FromFile<RunRequest>( filePath );
                }
                catch( Exception ) {
                    FileHelper.DeleteWithLock( filePath );
                }

                if( request != null ) {
                    var responseDirectory = CreateResponseDirectory( request.Id );
                    WriteResponse( request.Id, responseDirectory.FullName );

                    string summaryPath = Path.Combine( responseDirectory.FullName, FileNames.RunSummary );
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
                            FileHelper.MoveWithLock( filePath, Path.Combine( responseDirectory.FullName, FileNames.RunRequest ) );

                            runResult.Cases = request.Cases;

                            foreach( TestCase test in request.Cases ) {
                                var runTestResult = runResult.Cases.Single( t => t.Id == test.Id );

                                WriteTestStateFile( responseDirectory, runResult, false );

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

                        WriteTestStateFile( responseDirectory, runResult, true );

                        LogInfo( summaryPath, $"Test run end - duration {runResult.Timestamp - runResult.StartTime}" );

                    } );
                }
            }
        }

        private void LogInfo( string summaryPath, object message )
        {
            Log.Info( message );
            File.AppendAllText( summaryPath, message + "\n" );
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

            JsonHelper.ToFile( Path.Combine( runDirectory.FullName, FileNames.RunResult ), result );
        }

        private string GetNextRunRequestFile()
        {
            var files = Directory.GetFiles( FileNames.WatchDirectory, $"{FileNames.RunRequestBase}*.json" );
            return files.FirstOrDefault();
        }

        private string GetNextExploreRequestFile()
        {
            var files = Directory.GetFiles( FileNames.WatchDirectory, $"{FileNames.ExploreRequestBase}*.json" );
            return files.FirstOrDefault();
        }

        private DirectoryInfo CreateResponseDirectory( string id )
        {
            string directoryName = $"{id}";
            var directory = FileHelper.GetDirectory( Path.Combine( FileNames.WatchDirectory, directoryName ) );

            directory.Create();

            return directory;
        }
    }
}
