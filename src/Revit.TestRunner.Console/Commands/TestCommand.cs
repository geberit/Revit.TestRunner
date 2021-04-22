using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Client;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Communication.Dto;

namespace Revit.TestRunner.Console.Commands
{
    /// <summary>
    /// Execute Test Command
    /// </summary>
    [Verb( "test", HelpText = "Execute UnitTests" )]
    public class TestCommand : ICommand
    {
        /// <summary>
        /// Request File Path
        /// </summary>
        [Option( 'f', "file", Required = true, HelpText = "Request file path containing Tests to execute" )]
        public string RequestFile { get; set; }

        /// <summary>
        /// Preferred Revit Version.
        /// </summary>
        [Option( 'r', "revit", Default = 2020, HelpText = "Start Revit in Version" )]
        public int RevitVersion { get; set; }
        
        /// <summary>
        /// Execute Command.
        /// </summary>
        public void Execute()
        {
            if( !string.IsNullOrEmpty( RequestFile ) && File.Exists( RequestFile ) ) {
                var request = GetRequestFromFile( RequestFile );

                RunTests( request.Cases, RevitVersion.ToString() ).GetAwaiter().GetResult();
            }
            else {
                System.Console.WriteLine( "Request file not recognized." );
            }
        }

        private async Task RunTests( IEnumerable<TestCaseDto> cases, string revitVersion )
        {
            TimeSpan duration = TimeSpan.Zero;
            System.Console.WriteLine( $"App dir {FileNames.WatchDirectory}" );
            System.Console.WriteLine( $"Start test run {DateTime.Now}; preferred on Revit {revitVersion}" );
            System.Console.WriteLine();

            var complete = new List<TestCaseDto>();
            TestRunnerClient client = new TestRunnerClient( ConsoleConstants.ProgramName, ConsoleConstants.ProgramVersion );

            await client.StartTestRunAsync( cases, revitVersion, result => {
                try {
                    if( result.StateDto != null ) {
                        foreach( var test in result.StateDto.Cases.Where( c => c.State == TestState.Passed || c.State == TestState.Failed ) ) {
                            if( complete.All( t => t.Id != test.Id ) ) {
                                complete.Add( test );

                                string testString = $"{test.Id,-8} Test {test.State,-7} - {test.TestClass}.{test.MethodName}";

                                System.Console.WriteLine( testString );
                                if( !string.IsNullOrEmpty( test.Message ) ) System.Console.WriteLine( $"\t{test.Message}" );
                                if( !string.IsNullOrEmpty( test.StackTrace ) ) System.Console.WriteLine( $"\t{test.StackTrace}" );

                                duration = result.Duration;
                            }
                        }
                    }
                }
                catch( Exception e ) {
                    System.Console.WriteLine( $"Callback Exception: {e}" );

                }

                if( !string.IsNullOrEmpty( result.Message ) ) System.Console.WriteLine( result.Message );
            }, CancellationToken.None );

            int passedCount = complete.Count( t => t.State == TestState.Passed );

            System.Console.WriteLine();
            System.Console.WriteLine( $"Run finished - duration {duration:g} - {passedCount} of {complete.Count} Tests passed ({Math.Round( 100 * (double)passedCount / complete.Count )}%)" );

        }

        private TestRequestDto GetRequestFromFile( string path )
        {
            TestRequestDto request = null;

            if( File.Exists( path ) ) {
                try {
                    request = JsonHelper.FromFile<TestRequestDto>( path );

                    System.Console.WriteLine( $"Request loaded from '{path}'" );
                }
                catch( Exception e ) {
                    System.Console.WriteLine( $"Can not create Request from '{path}' - {e}" );
                }
            }
            else {
                System.Console.WriteLine( $"File does not exist '{path}'" );
            }

            return request;
        }
    }
}
