using System.IO;
using CommandLine;

namespace Revit.TestRunner.Console.Commands
{
    /// <summary>
    /// Execute Test Command
    /// </summary>
    [Verb( "all", HelpText = "Execute all UnitTests from a specified assembly" )]
    public class AssemblyCommand : ICommand
    {
        /// <summary>
        /// Request File Path
        /// </summary>
        [Option( 'a', "assembly", Required = true, HelpText = "Assembly path containing Tests to execute" )]
        public string AssemblyPath { get; set; }

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
            if( !string.IsNullOrEmpty( AssemblyPath ) && File.Exists( AssemblyPath ) ) {
                //await RunAll( AssemblyPath, RevitVersion.ToString() );
                System.Console.WriteLine( "Not implemented yet." );
            }
            else {
                System.Console.WriteLine( "Request file not recognized." );
            }
        }
    }
}
