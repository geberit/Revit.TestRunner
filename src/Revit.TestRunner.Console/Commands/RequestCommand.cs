using System;
using System.IO;
using CommandLine;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Communication.Dto;

namespace Revit.TestRunner.Console.Commands
{
    /// <summary>
    /// Execute Test Command
    /// </summary>

    [Verb( "request", HelpText = "Execute UnitTests in Revit, specified in a request file" )]
    public class RequestCommand : AbstractTestCommand
    {
        /// <summary>
        /// Request File Path
        /// </summary>
        [Value( 0, HelpText = "Request file path containing Tests specifications to execute" )]
        public string RequestFile { get; set; }


        /// <summary>
        /// Execute Command.
        /// </summary>
        public override void Execute()
        {
            if( FileExist( RequestFile ) ) {
                System.Console.WriteLine( $"Run tests from request file '{RequestFile}'" );
                System.Console.WriteLine( $"App dir {FileNames.WatchDirectory}" );

                System.Console.WriteLine( "Get requests from file" );
                var request = GetRequestFromFile( RequestFile );

                RunTests( request.Cases ).GetAwaiter().GetResult();
            }
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
