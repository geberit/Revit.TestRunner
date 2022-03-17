using System;
using CommandLine;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Dto;

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
            base.Execute();

            if( FileExist( RequestFile ) ) {
                System.Console.WriteLine( "Run tests from request file" );
                System.Console.WriteLine( $"Get requests from file '{RequestFile}'" );

                var request = GetRequestFromFile( RequestFile );
                var allPassed = RunTests( request.Cases ).GetAwaiter().GetResult();

                if( !allPassed ) {
                    Environment.Exit( -1 );
                }
            }
        }

        /// <summary>
        /// Get request from file.
        /// </summary>
        private TestRequestDto GetRequestFromFile( string path )
        {
            TestRequestDto request = null;

            try {
                request = JsonHelper.FromFile<TestRequestDto>( path );
            }
            catch( Exception e ) {
                System.Console.WriteLine( $"Can not create Request from '{path}' - {e}" );
            }


            return request;
        }
    }
}