using CommandLine;

namespace Revit.TestRunner.Console.Commands
{
    public interface ICommand
    {
        void Execute();
    }

    [Verb( "hello", HelpText = "Say Hello" )]
    public class HelloCommand : ICommand
    {
        public void Execute()
        {
            System.Console.WriteLine( "Hello, thank you for using Revit.TestRunner" );
        }
    }
}
