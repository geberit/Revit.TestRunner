using System;
using System.IO;

namespace Revit.TestRunner.Shared.Communication
{
    public static class FileNames
    {
        public static string WatchDirectory => Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), "Revit.TestRunner" );

        private const string RunnerStatusFileName = "status.json";

        public const string RunResult = "result.json";
        public const string RunRequest = "request.json";
        public const string RunSummary = "summary.txt";

        private const string ResponseBase = "response";

        public const string RunRequestBase = "request_run";
        public const string ExploreRequestBase = "request_explore";

        public const string ExploreResponseFileName = "response.json";
        public const string ExploreResultFileName = "explore.xml";

        public static string RunnerStatusFilePath => Path.Combine( WatchDirectory, RunnerStatusFileName );

        public static string ResponseFilePath( string aId )
        {
            if( string.IsNullOrEmpty( aId ) ) throw new ArgumentNullException( nameof( aId ), "Argument must be a valid string" );
            return Path.Combine( WatchDirectory, $"{ResponseBase}_{aId}.json" );
        }

        public static string RunRequestFilePath( string aId )
        {
            if( string.IsNullOrEmpty( aId ) ) throw new ArgumentNullException( nameof( aId ), "Argument must be a valid string" );
            return Path.Combine( WatchDirectory, $"{RunRequestBase}_{aId}.json" );
        }

        public static string ExploreRequestFilePath( string aId )
        {
            if( string.IsNullOrEmpty( aId ) ) throw new ArgumentNullException( nameof( aId ), "Argument must be a valid string" );
            return Path.Combine( WatchDirectory, $"{ExploreRequestBase}_{aId}.json" );
        }
    }
}
