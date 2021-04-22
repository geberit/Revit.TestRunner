using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Client;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Communication.Dto;

namespace Revit.TestRunner.Console
{
    /// <summary>
    /// Revit.TestRunner console application.
    /// Pass test request files to the service and get results.
    /// </summary>
    public class Program
    {
        private string ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private const string ProgramName = "ConsoleRunner";

        private static string[] TestArgs = new[] { "2020", @"C:\temp\TestRequest.json" };

        public static void Main( string[] args )
        {
            if( args == null || args.Length == 0 ) args = TestArgs;

            Program program = new Program();
            program.MainAsync( args ).GetAwaiter().GetResult();

            System.Console.ReadKey();
        }

        private async Task MainAsync( string[] args )
        {
            TestRequestDto request = null;
            string requestFile = GetFile( args );
            string revitVersion = GetVersion( args );

            if( !string.IsNullOrEmpty( requestFile ) ) {
                request = GetRequestFromFile( requestFile );
            }

            await RunTests( request.Cases, revitVersion );
        }

        private string GetVersion( string[] args )
        {
            string result = "2020";

            if( args != null ) {
                foreach( var arg in args ) {
                    if( !string.IsNullOrEmpty( arg ) ) {
                        if( int.TryParse( arg, out int i ) ) {
                            result = i.ToString();
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private string GetFile( string[] args )
        {
            string result = string.Empty;

            if( args != null ) {
                foreach( var arg in args ) {
                    if( !string.IsNullOrEmpty( arg ) ) {
                        if( File.Exists( arg ) ) {
                            result = arg;
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private async Task RunTests( IEnumerable<TestCaseDto> cases, string aRevitVersion )
        {
            TimeSpan duration = TimeSpan.Zero;
            System.Console.WriteLine( $"App dir {FileNames.WatchDirectory}" );
            System.Console.WriteLine( $"Start test run {DateTime.Now}; preferred on Revit {aRevitVersion}" );
            System.Console.WriteLine();

            var complete = new List<TestCaseDto>();
            TestRunnerClient client = new TestRunnerClient( ProgramName, ProgramVersion );

            await client.StartTestRunAsync( cases, aRevitVersion, result => {
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
