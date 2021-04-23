using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Console.Commands
{
    /// <summary>
    /// Acbstract Command for run tests.
    /// </summary>
    public abstract class AbstractTestCommand : ICommand
    {
        /// <summary>
        /// Preferred Revit Version.
        /// </summary>
        [Option( 'r', "revit", Default = 2020, HelpText = "Start Revit in Version" )]
        public int RevitVersion { get; set; }

        /// <summary>
        /// Execute Command.
        /// </summary>
        public virtual void Execute()
        {
            System.Console.WriteLine( $"App dir '{FileNames.WatchDirectory}'" );
        }

        /// <summary>
        /// Validate File existance. Write to console if not.
        /// </summary>
        protected bool FileExist( string filePath )
        {
            bool result = false;
            if( !string.IsNullOrEmpty( filePath ) && File.Exists( filePath ) ) {
                result = true;
            }
            else {
                System.Console.WriteLine( "Input file not found!" );
            }

            return result;
        }

        /// <summary>
        /// Run specified Tests.
        /// </summary>
        protected async Task RunTests( IEnumerable<TestCaseDto> cases )
        {
            System.Console.WriteLine( $"Start test run {DateTime.Now}; preferred on Revit {RevitVersion}" );
            System.Console.WriteLine();
            TimeSpan duration = TimeSpan.Zero;

            var complete = new List<TestCaseDto>();
            var client = new TestRunnerClient( ConsoleConstants.ProgramName, ConsoleConstants.ProgramVersion );

            await client.StartTestRunAsync( cases, RevitVersion.ToString(), result => {
                try {
                    if( result.StateDto != null ) {
                        foreach( var test in result.StateDto.Cases.Where( c => c.State != TestState.Unknown ) ) {
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
            int completeCount = complete.Count( t => t.State == TestState.Passed || t.State == TestState.Failed );

            System.Console.WriteLine();
            System.Console.WriteLine( $"Run finished - duration {duration:g} - {passedCount} of {completeCount} Tests passed ({Math.Round( 100 * (double)passedCount / completeCount )}%)" );

        }
    }
}
