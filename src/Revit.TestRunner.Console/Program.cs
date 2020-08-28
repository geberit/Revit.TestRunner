using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Client;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Console
{
    public class Program
    {
        private const string ArgRequestFile = "request";
        private const string SampleFile = @"C:\Users\floto\AppData\Roaming\Revit.TestRunner\_samples\sampleFake.json";
        private const string SamplePlugin1 = @"C:\Users\floto\AppData\Roaming\Revit.TestRunner\_samples\PluginTests1.json";

        private readonly string mWatchDirectory = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Revit.TestRunner" );

        public static void Main( string[] args )
        {
            Program program = new Program();
            program.MainAsync( args ).GetAwaiter().GetResult();

            System.Console.ReadKey();
        }

        private async Task MainAsync( string[] args )
        {
            if( args.Any() ) {
                if( args[0] == ArgRequestFile ) {
                    if( args.Length != 2 ) System.Console.WriteLine( $"Argument '{ArgRequestFile}' need request json file" );

                    var request = GetRequestFromFile( args[1] );
                    await RunTests( request );
                }
            }
            else {
                //var request = GetSampleRequest();
                var request = GetRequestFromFile( SamplePlugin1 );
                await RunTests( request );
            }
        }

        private async Task RunTests( RunRequest request )
        {
            request.Id = Client.GenerateId();
            request.ClientName = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;
            request.ClientVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            TimeSpan duration = TimeSpan.Zero;
            System.Console.WriteLine( $"Start test run {DateTime.Now}" );

            var complete = new List<TestCase>();
            Client client = new Client( mWatchDirectory );

            await client.RunAsync( request, result => {
                try {
                    foreach( var test in result.Result.Cases.Where( c => c.State == TestState.Passed || c.State == TestState.Failed ) ) {
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
                catch( Exception e ) {
                    System.Console.WriteLine( $"Callback Exception: {e}" );

                }
            } );

            int passedCount = complete.Count( t => t.State == TestState.Passed );

            System.Console.WriteLine();
            System.Console.WriteLine( $"Run finished - duration {duration:g} - {passedCount} of {complete.Count} Tests passed ({Math.Round( 100 * (double)passedCount / complete.Count )}%)" );

        }

        private RunRequest GetRequestFromFile( string path )
        {
            RunRequest request = null;

            if( File.Exists( path ) ) {
                try {
                    request = JsonHelper.FromFile<RunRequest>( path );

                    System.Console.WriteLine( $"Request loaded from '{path}'" );
                }
                catch( Exception e) {
                    System.Console.WriteLine( $"Can not create Request from '{path}' - {e}" );
                }
            }
            else {
                System.Console.WriteLine( $"File does not exist '{path}'" );
            }

            return request;
        }

        private RunRequest GetSampleRequest()
        {
            var dir = Directory.GetCurrentDirectory();
            string pathToSampleRequest = Path.Combine( dir, "sampleMoreTests.json" );

            RunRequest request = GetRequestFromFile( pathToSampleRequest );
            request.Id = "SampleRun";

            foreach( TestCase testCase in request.Cases ) {
                testCase.AssemblyPath = Path.Combine( dir, "Revit.TestRunner.SampleTestProject.dll" );
            }

            return request;
        }
    }
}
