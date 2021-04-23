using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Revit.TestRunner.Shared;
using Revit.TestRunner.Shared.Communication;
using Revit.TestRunner.Shared.Model;
using Revit.TestRunner.Shared.NUnit;

namespace Revit.TestRunner.Console.Commands
{
    /// <summary>
    /// Execute Test Command
    /// </summary>
    [Verb( "assembly", HelpText = "Execute all UnitTests from a specified assembly" )]
    public class AssemblyCommand : AbstractTestCommand
    {
        /// <summary>
        /// Request File Path
        /// </summary>
        [Value( 0, HelpText = "Assembly path containing Tests to execute" )]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// Execute Command.
        /// </summary>
        public override void Execute()
        {
            if( FileExist( AssemblyPath ) ) {
                System.Console.WriteLine( $"Run all tests in assembly '{AssemblyPath}'" );
                System.Console.WriteLine( $"App dir {FileNames.WatchDirectory}" );

                RunAll( AssemblyPath ).GetAwaiter().GetResult();
            }
        }

        private async Task RunAll( string assemblyPath )
        {
            System.Console.WriteLine( "Explore assembly" );
            TestRunnerClient client = new TestRunnerClient( ConsoleConstants.ProgramName, ConsoleConstants.ProgramVersion );
            var explore = await client.ExploreAssemblyAsync( assemblyPath, RevitVersion.ToString(), CancellationToken.None );

            System.Console.WriteLine( "Get tests from assembly" );
            var root = ModelHelper.ToNodeTree( explore.ExploreFile );

            var cases = root.DescendantsAndMe.Where( n => n.Type == TestType.Case ).ToArray();

            await RunTests( cases.Select( ModelHelper.ToTestCase ) );
        }
    }
}
