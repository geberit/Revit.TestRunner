using System;

namespace Revit.TestRunner.Shared.Dto
{
    public class RunnerStatus
    {
        public DateTime Timestamp { get; set; }

        public string CurrentRun { get; set; }

        public string LogFilePath { get; set; }
    }
}
