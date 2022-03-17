using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Revit.TestRunner.Shared;
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
            base.Execute();

            var allPassed = false;
            if( FileExist( AssemblyPath ) ) {
                allPassed = RunAll( AssemblyPath ).GetAwaiter().GetResult();
            }

            if( !allPassed ) {
                System.Environment.Exit( -1 );
            }
        }

        /// <summary>
        /// Run all tests in assembly. Returns true if tests could be loaded and all passed.
        /// </summary>
        private async Task<bool> RunAll( string assemblyPath )
        {
            System.Console.WriteLine( "Run all tests in assembly" );
            System.Console.WriteLine( $"Explore assembly '{AssemblyPath}'" );

            TestRunnerClient client = new TestRunnerClient( ConsoleConstants.ProgramName, ConsoleConstants.ProgramVersion );
            var explore = await client.ExploreAssemblyAsync( assemblyPath, RevitVersion.ToString(), RevitLanguage, CancellationToken.None );

            if( explore != null ) {
                System.Console.WriteLine( "Get tests from assembly" );
                var root = ModelHelper.ToNodeTree( explore.ExploreFile );
                var cases = root.DescendantsAndMe.Where( n => n.Type == TestType.Case ).ToArray();

                return await RunTests( cases.Select( ModelHelper.ToTestCase ), client );
            }

            return false;
        }
    }
}