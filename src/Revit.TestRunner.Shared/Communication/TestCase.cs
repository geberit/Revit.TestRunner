namespace Revit.TestRunner.Shared.Communication
{
    /// <summary>
    /// Represents a TestCase.
    /// </summary>
    public class TestCase
    {
        public string Id { get; set; }

        public string AssemblyPath { get; set; }

        public string TestClass { get; set; }

        public string MethodName { get; set; }

        public TestState State { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }
    }
}
