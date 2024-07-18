using System;
using System.IO;

namespace Revit.TestRunner.Shared.Communication
{
    public static class FileNames
    {
        public static string WatchDirectory => Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Revit.TestRunner" );

        public const string RunResult = "result.json";
        public const string RunResultXml = "result.xml";
        public const string RunSummary = "summary.txt";

        public const string ExploreResultFileName = "explore.xml";
    }
}
