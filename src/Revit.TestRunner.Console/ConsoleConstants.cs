using System.Reflection;

namespace Revit.TestRunner.Console
{
    internal class ConsoleConstants
    {
        internal const string ProgramName = "ConsoleRunner";
        internal static string ProgramVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
