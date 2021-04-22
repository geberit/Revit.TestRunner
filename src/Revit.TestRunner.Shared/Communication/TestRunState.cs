﻿using System;
using Revit.TestRunner.Shared.Communication.Dto;

namespace Revit.TestRunner.Shared.Client
{
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