using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Communication.Client;
using Revit.TestRunner.Shared.Dto;
using Revit.TestRunner.Shared.Model;

namespace Revit.TestRunner.Shared
{
    public class TestRunnerClient
    {
        #region Members, Constructor

        private readonly FileClient mFileClient;
        private HomeDto mHome;
        private bool mNewRevit;

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
        private async Task<HomeDto> Home( CancellationToken cancellationToken )
        {
            mHome = await mFileClient.GetJson<HomeDto>( "", cancellationToken, 30, 2000 );
            return mHome;
        }

        /// <summary>
        /// Start a expolre request.
        /// </summary>
        public async Task<ExploreResponseDto> ExploreAssemblyAsync( string assemblyPath, string revitVersion, string revitLanguage, CancellationToken cancellationToken )
        {
            ExploreResponseDto result = null;

            var request = new ExploreRequestDto {
                AssemblyPath = assemblyPath
            };

            var revit = RevitHelper.StartRevit( revitVersion, revitLanguage );
            mNewRevit |= revit.IsNew;

            await Home( cancellationToken );

            if( mHome != null ) {
                result = await mFileClient.GetJson<ExploreRequestDto, ExploreResponseDto>( mHome.ExplorePath, request, cancellationToken );
            }
            else {
                Console.WriteLine( "TestRunner service not available!" );
            }

            return result;
        }

        /// <summary>
        /// Start a test run request.
        /// </summary>
        public async Task StartTestRunAsync( IEnumerable<TestCaseDto> testCases, string revitVersion, string revitLanguage, Action<TestRunState> callback, CancellationToken cancellationToken )
        {
            var request = new TestRequestDto {
                Cases = testCases.ToArray()
            };

            var revit = RevitHelper.StartRevit( revitVersion, revitLanguage );
            mNewRevit |= revit.IsNew;

            await Home( cancellationToken );

            if( mHome != null ) {
                TestResponseDto response = await mFileClient.GetJson<TestRequestDto, TestResponseDto>( mHome.TestPath, request, cancellationToken );

                if( response != null ) {
                    var resultFile = response.ResultFile;

                    // Wait resultFile 
                    for( int i = 0; i < 10; i++ )
                        if( !File.Exists( resultFile ) ) {
                            Console.WriteLine( $"." ); // Wait resultFile
                            await Task.Delay( 200, cancellationToken );
                        }

                    if( File.Exists( resultFile ) ) {
                        bool run = true;

                        while( run && !cancellationToken.IsCancellationRequested ) {
                            var runResult = JsonHelper.FromFile<TestRunStateDto>( resultFile );

                            if( runResult != null ) {
                                bool isCompleted = runResult.State == TestState.Passed || runResult.State == TestState.Failed;
                                TestRunState result = new TestRunState( runResult, isCompleted ) { Message = runResult.Output };

                                callback( result );

                                run = !isCompleted;

                                if( run ) await Task.Delay( 500, cancellationToken );
                            }
                        }
                    }
                    else {
                        callback( new TestRunState( null, true ) { Message = "Tests not executed! Service may not be running." } );
                    }

                    if( mNewRevit ) RevitHelper.KillRevit( revit.ProcessId );
                }
            }
            else {
                callback( new TestRunState( null, true ) { Message = "TimeOut. Runner not available!" } );
            }
        }

        #endregion
    }
}