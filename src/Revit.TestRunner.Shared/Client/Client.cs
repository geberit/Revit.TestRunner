using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Shared.Client
{
    /// <summary>
    /// Revit.TestRunner client to interact with service.
    /// Communication is over json files in watch directory.
    /// </summary>
    public class Client
    {
        #region Members, Constructor

        private readonly string mClientName;
        private readonly string mClientVersion;

        /// <summary>
        /// Constructor
        /// </summary>
        public Client( string aClientName = "", string aClientVersion = "" )
        {
            mClientName = aClientName;
            mClientVersion = aClientVersion;

            ClearRunnerStatus();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Start loop of observing the watch directory. 
        /// </summary>
        public void StartRunnerStatusWatcher( Action<RunnerStatus> aCallback, CancellationToken aCancellationToken )
        {
            while( !aCancellationToken.IsCancellationRequested ) {
                RunnerStatus status = CheckStatus();

                Task.Delay( 1000, aCancellationToken );

                aCallback( status );
            }
        }

        /// <summary>
        /// Check if a Revit.TestRunner service is available. Timeout 120.
        /// </summary>
        private async Task<bool> IsRunnerAvailable( CancellationToken aCancellationToken )
        {
            bool result = false;

            ClearRunnerStatus();

            for( int i = 0; i < 120; i++ ) {
                try {
                    var status = CheckStatus();
                    result = status != null;

                    if( result ) break;
                    if( aCancellationToken.IsCancellationRequested ) break;

                    await Task.Delay( 1000, aCancellationToken );
                }
                catch {
                    // do nothing
                }
            }

            return result;
        }

        /// <summary>
        /// Get service status file from watch directory.
        /// </summary>
        private RunnerStatus CheckStatus()
        {
            RunnerStatus status = null;

            try {
                status = JsonHelper.FromFile<RunnerStatus>( FileNames.RunnerStatusFilePath );
            }
            catch {
                // do nothing
            }

            return status;
        }

        /// <summary>
        /// Start a expolre request.
        /// </summary>
        public async Task<ExploreResponse> ExploreAssemblyAsync( string aAssemblyPath, string aRevitVersion, CancellationToken aCancellationToken )
        {
            ExploreResponse response = null;
            ExploreRequest request = new ExploreRequest {
                Timestamp = DateTime.Now,
                Id = GenerateId(),
                ClientName = mClientName,
                ClientVersion = mClientVersion,
                AssemblyPath = aAssemblyPath
            };

            RevitHelper.StartRevit( aRevitVersion );
            bool isRunnerAvailable = await IsRunnerAvailable( aCancellationToken );

            if( isRunnerAvailable ) {
                string requestFilePath = FileNames.ExploreRequestFilePath( request.Id );
                JsonHelper.ToFile( requestFilePath, request );

                var responseDirectoryPath = await GetResponseDirectory( request.Id );

                if( Directory.Exists( responseDirectoryPath ) ) {
                    while( response == null && !aCancellationToken.IsCancellationRequested ) {
                        string responseFile = Path.Combine( responseDirectoryPath, FileNames.ExploreResponseFileName );
                        response = JsonHelper.FromFile<ExploreResponse>( responseFile );

                        if( response == null ) {
                            await Task.Delay( 500, aCancellationToken );
                        }
                    }
                }
                else {
                    FileHelper.DeleteWithLock( requestFilePath );
                }
            }

            return response;
        }

        /// <summary>
        /// Start a test run request.
        /// </summary>
        public async Task StartTestRunAsync( IEnumerable<TestCase> aTestCases, string aRevitVersion, Action<ProcessResult> aCallback, CancellationToken aCancellationToken )
        {
            RunRequest request = new RunRequest {
                Cases = aTestCases.ToArray()
            };

            await StartTestRunAsync( request, aRevitVersion, aCallback, aCancellationToken );
        }

        /// <summary>
        /// Start a test run request.
        /// </summary>
        public async Task StartTestRunAsync( RunRequest aRequest, string aRevitVersion, Action<ProcessResult> aCallback, CancellationToken aCancellationToken )
        {
            aRequest.Timestamp = DateTime.Now;
            aRequest.Id = GenerateId();
            aRequest.ClientName = mClientName;
            aRequest.ClientVersion = mClientVersion;

            var revit = RevitHelper.StartRevit( aRevitVersion );
            bool isRunnerAvailable = await IsRunnerAvailable( aCancellationToken );

            if( isRunnerAvailable ) {
                string requestFilePath = FileNames.RunRequestFilePath( aRequest.Id );
                JsonHelper.ToFile( requestFilePath, aRequest );

                var responseDirectoryPath = await GetResponseDirectory( aRequest.Id );

                if( Directory.Exists( responseDirectoryPath ) ) {
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
                    FileHelper.DeleteWithLock( requestFilePath );
                    aCallback( new ProcessResult( null, true ) { Message = "Tests not executed! Service may not be running." } );
                }

                if( revit.IsNew ) RevitHelper.KillRevit( revit.ProcessId );
            }
            else {
                aCallback( new ProcessResult( null, true ) { Message = "TimeOut. Runner not available!" } );
            }
        }

        /// <summary>
        /// Get the specific response directory from the <see cref="Response"/> message.
        /// </summary>
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

        /// <summary>
        /// Clear server status information.
        /// </summary>
        private void ClearRunnerStatus()
        {
            FileHelper.DeleteWithLock( FileNames.RunnerStatusFilePath );
        }

        /// <summary>
        /// Generate a (kind of unique) id.
        /// </summary>
        private static string GenerateId()
        {
            Random r = new Random();
            r.Next( 1000, 9999 );
            int number = r.Next( 1000, 9999 );
            return $"{DateTime.Now:yyyyMMdd-HHmmss}_{number}";
        }
        #endregion
    }
}
