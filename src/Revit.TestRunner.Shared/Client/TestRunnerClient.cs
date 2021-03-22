using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Communication.Client;
using Revit.TestRunner.Shared.Communication.Dto;

namespace Revit.TestRunner.Shared.Client
{
    public class TestRunnerClient
    {
        #region Members, Constructor

        private readonly FileClient mFileClient;
        private HomeDto mHome;

        /// <summary>
        /// Constructor
        /// </summary>
        public TestRunnerClient( string aClientName = "", string aClientVersion = "" )
        {
            mFileClient = new FileClient( FileNames.WatchDirectory, aClientName, aClientVersion );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Start loop of calling home. 
        /// </summary>
        public void StartRunnerStatusWatcher( Action<HomeDto> aCallback, CancellationToken cancellationToken )
        {
            Task.Run( async () => {
                while( !cancellationToken.IsCancellationRequested && mHome == null ) {
                    mHome = await Home( cancellationToken );
                    aCallback( mHome );

                    Thread.Sleep( 1000 );
                }
            }, cancellationToken );
        }

        /// <summary>
        /// Call Home
        /// </summary>
        public async Task<HomeDto> Home( CancellationToken cancellationToken )
        {
            mHome = await mFileClient.GetJson<HomeDto>( "", cancellationToken );
            return mHome;
        }

        /// <summary>
        /// Start a expolre request.
        /// </summary>
        public async Task<ExploreResponseDto> ExploreAssemblyAsync( string assemblyPath, string revitVersion, CancellationToken cancellationToken )
        {
            ExploreResponseDto result = null;

            var request = new ExploreRequestDto {
                AssemblyPath = assemblyPath
            };

            RevitHelper.StartRevit( revitVersion );

            if( mHome != null ) {
                result = await mFileClient.GetJson<ExploreRequestDto, ExploreResponseDto>( mHome.ExplorePath, request, cancellationToken );
            }

            return result;
        }

        /// <summary>
        /// Start a test run request.
        /// </summary>
        public async Task StartTestRunAsync( IEnumerable<TestCase> testCases, string revitVersion, Action<ProcessResult> callback, CancellationToken cancellationToken )
        {
            var request = new TestRequestDto {
                Cases = testCases.ToArray()
            };

            var revit = RevitHelper.StartRevit( revitVersion );

            if( mHome != null ) {
                TestResponseDto response = await mFileClient.GetJson<TestRequestDto, TestResponseDto>( mHome.TestPath, request, cancellationToken );

                if( response != null ) {
                    var responseDirectoryPath = response.ResponseDirectory;

                    if( Directory.Exists( responseDirectoryPath ) ) {
                        bool run = true;

                        while( run && !cancellationToken.IsCancellationRequested ) {
                            var runResult = JsonHelper.FromFile<RunResult>( Path.Combine( responseDirectoryPath, FileNames.RunResult ) );

                            if( runResult != null ) {
                                bool isCompleted = runResult.State == TestState.Passed || runResult.State == TestState.Failed;
                                ProcessResult result = new ProcessResult( runResult, isCompleted );

                                callback( result );

                                run = !isCompleted;

                                if( run ) await Task.Delay( 500, cancellationToken );
                            }
                        }
                    }
                    else {
                        callback( new ProcessResult( null, true ) { Message = "Tests not executed! Service may not be running." } );
                    }

                    if( revit.IsNew ) RevitHelper.KillRevit( revit.ProcessId );

                }
            }
            else {
                callback( new ProcessResult( null, true ) { Message = "TimeOut. Runner not available!" } );
            }

        }
        #endregion

    }
}
