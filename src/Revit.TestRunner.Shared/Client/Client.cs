using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Shared.Client
{
    public class Client
    {
        private readonly string mClientName;
        private readonly string mClientVersion;

        public Client( string aClientName = "", string aClientVersion = "" )
        {
            mClientName = aClientName;
            mClientVersion = aClientVersion;
        }

        public async Task<ExploreResponse> ExploreAssemblyAsync( string aAssemblyPath, CancellationToken aCancellationToken )
        {
            ExploreResponse response = null;
            ExploreRequest request = new ExploreRequest {
                Timestamp = DateTime.Now,
                Id = GenerateId(),
                ClientName = mClientName,
                ClientVersion = mClientVersion,
                AssemblyPath = aAssemblyPath
            };

            string requestFilePath = FileNames.ExploreRequestFilePath( request.Id );
            JsonHelper.ToFile( requestFilePath, request );

            var responseDirectoryPath = await GetResponseDirectory( request.Id );

            if( Directory.Exists( responseDirectoryPath ) ) {
                while( response == null && !aCancellationToken.IsCancellationRequested ) {
                    string responseFile = Path.Combine(responseDirectoryPath, FileNames.ExploreResponseFileName);
                    response = JsonHelper.FromFile<ExploreResponse>( responseFile );

                    if( response == null ) {
                        await Task.Delay( 500, aCancellationToken );
                    }
                }
            }
            else {
                FileHelper.DeleteWithLock( requestFilePath );
            }

            return response;
        }

        public async Task StartTestRunAsync( IEnumerable<TestCase> aTestCases, Action<ProcessResult> aCallback, CancellationToken aCancellationToken )
        {
            RunRequest request = new RunRequest {
                Cases = aTestCases.ToArray()
            };

            await StartTestRunAsync( request, aCallback, aCancellationToken );
        }

        public async Task StartTestRunAsync( RunRequest aRequest, Action<ProcessResult> aCallback, CancellationToken aCancellationToken )
        {
            aRequest.Timestamp = DateTime.Now;
            aRequest.Id = GenerateId();
            aRequest.ClientName = mClientName;
            aRequest.ClientVersion = mClientVersion;

            string requestFilePath = FileNames.RunRequestFilePath( aRequest.Id );
            JsonHelper.ToFile( requestFilePath, aRequest );

            var responseDirectoryPath = await GetResponseDirectory( aRequest.Id );

            if( !string.IsNullOrEmpty( responseDirectoryPath ) ) {
                bool run = true;

                while( run && !aCancellationToken.IsCancellationRequested ) {
                    var runResult = JsonHelper.FromFile<RunResult>( Path.Combine( responseDirectoryPath, FileNames.RunResult ) );

                    if( runResult != null ) {
                        bool isCompleted = runResult.State == TestState.Passed || runResult.State == TestState.Failed;
                        ProcessResult result = new ProcessResult( runResult, isCompleted );

                        aCallback( result );

                        run = !isCompleted;

                        if( run ) await Task.Delay( 500, aCancellationToken );
                    }
                }
            }
            else {
                aCallback( new ProcessResult( null, true ) { Message = "No run directory!" } );
            }
        }


        private async Task<string> GetResponseDirectory( string aId )
        {
            string result = string.Empty;
            string responseFileName = FileNames.ResponseFilePath( aId );

            for( int i = 0; i < 10; i++ ) {
                var response = JsonHelper.FromFile<Response>( responseFileName );

                if( response != null && response.Id == aId ) {
                    result = response.Directory;
                    FileHelper.DeleteWithLock( responseFileName );

                    break;
                }

                await Task.Delay( 1000 );
            }

            return result;
        }

        private static string GenerateId()
        {
            Random r = new Random();
            r.Next( 1000, 9999 );
            int number = r.Next( 1000, 9999 );
            return $"{DateTime.Now:yyyyMMdd-HHmmss}_{number}";
        }
    }
}
