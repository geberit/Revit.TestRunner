using System;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Shared.Client
{
    public class ProcessResult
    {
        public ProcessResult( RunResult result, bool isCompleted )
        {
            Result = result;
            IsCompleted = isCompleted;
        }

        public RunResult Result { get; }

        public bool IsCompleted { get; }

        public TimeSpan Duration => Result != null ? Result.Timestamp - Result.StartTime : TimeSpan.Zero;

        public string Message { get; internal set; }
    }
}
