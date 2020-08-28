using System;
using System.IO;
using System.Threading.Tasks;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Shared.Client
{
    public class Client
    {
        private readonly DirectoryInfo mWatchDirectory;

        public Client( string aWatchDirectory )
        {
            mWatchDirectory = new DirectoryInfo( aWatchDirectory );

        }

        public async Task RunAsync( RunRequest aRequest, Action<ProcessResult> aCallback )
        {
            string requestFilePath = Path.Combine( mWatchDirectory.FullName, $"{aRequest.Id}.json" );
            JsonHelper.ToFile( requestFilePath, aRequest );

            var runDirectoryPath = await GetRunDirectory( aRequest.Id );

            if( !string.IsNullOrEmpty( runDirectoryPath ) ) {
                bool run = true;

                while( run ) {
                    var runResult = JsonHelper.FromFile<RunResult>( Path.Combine( runDirectoryPath, "result.json" ) );

                    if( runResult != null ) {
                        bool isCompleted = runResult.State == TestState.Passed || runResult.State == TestState.Failed;
                        ProcessResult result = new ProcessResult( runResult, isCompleted );

                        aCallback( result );

                        run = !isCompleted;
                    }
                }
            }
            else {
                aCallback( new ProcessResult( null, true ) { Message = "No run directory!" } );
            }
        }


        private async Task<string> GetRunDirectory( string aId )
        {
            string result = string.Empty;

            for( int i = 0; i < 10; i++ ) {
                var status = JsonHelper.FromFile<RunnerStatus>( Path.Combine( mWatchDirectory.FullName, "status.json" ) );

                if( status != null && !string.IsNullOrEmpty( status.CurrentRun ) && status.CurrentRun.Contains( aId ) ) {
                    result = status.CurrentRun;
                    break;
                }

                await Task.Delay( 1000 );
            }

            return result;
        }

        public static string GenerateId()
        {
            Random r = new Random();
            r.Next( 1000, 9999 );
            int number = r.Next( 1000, 9999 );
            return $"{DateTime.Now:yyyyMMdd-hhmmss}-{number}";
        }
    }
}
