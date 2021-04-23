using System;
using Revit.TestRunner.Shared.Dto;

namespace Revit.TestRunner.Shared.Model
{
    /// <summary>
    /// Test state object for callback.
    /// </summary>
    public class TestRunState
    {
        public TestRunState( TestRunStateDto stateDto, bool isCompleted )
        {
            StateDto = stateDto;
            IsCompleted = isCompleted;
        }

        public TestRunStateDto StateDto { get; }

        public bool IsCompleted { get; }

        public TimeSpan Duration => StateDto != null ? StateDto.Timestamp - StateDto.StartTime : TimeSpan.Zero;

        public string Message { get; internal set; }
    }
}
