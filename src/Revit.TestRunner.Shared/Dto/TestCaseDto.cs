using System;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Represents a TestCase / Test method.
    /// </summary>
    public class TestCaseDto
    {
        public string Id { get; set; }

        public string AssemblyPath { get; set; }

        public string TestClass { get; set; }

        public string MethodName { get; set; }

        public TestState State { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
    }
}
