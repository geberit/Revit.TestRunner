using System.Diagnostics;
using CommandLine;
using Revit.TestRunner.Console.Commands;

namespace Revit.TestRunner.Console
{
    /// <summary>
    /// Revit.TestRunner console application.
    /// Pass test request files to the service and get results.
    /// </summary>
    public class Program
    {
        public static void Main( string[] args )
        {
            //if( Debugger.IsAttached ) args = new[] { "--help" };
            if( Debugger.IsAttached ) args = new [] { "test", "-r", "2020", "-f", @"C:\Users\floto\Desktop\App.json" };

            Parser.Default.ParseArguments<TestCommand, AssemblyCommand, HelloCommand>( args )
                .WithParsed<ICommand>( t => t.Execute() );

            if( Debugger.IsAttached ) System.Console.ReadKey();
        }
    }
}
