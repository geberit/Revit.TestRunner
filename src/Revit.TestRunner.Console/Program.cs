using System.Diagnostics;
using System.IO;
using System.Reflection;
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
            if( Debugger.IsAttached ) args = new[] { "--help" };
            //if( Debugger.IsAttached ) args = new [] { "request", @"C:\temp\App.json", "-r", "2020" };
            if( Debugger.IsAttached ) {
                var assembly = new FileInfo( Assembly.GetExecutingAssembly().Location );
                var testAssemblyPath = Path.Combine( assembly.Directory.Parent.FullName, "Revit.TestRunner.SampleTestProject2.dll" );
                args = new[] { "assembly", testAssemblyPath, "-r", "2020", };
            }

            Parser.Default.ParseArguments<RequestCommand, AssemblyCommand, HelloCommand>( args )
                .WithParsed<ICommand>( t => t.Execute() );

            //System.Console.ReadKey();
        }
    }
}