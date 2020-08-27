namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Represents a test run, containing a set of <see cref="TestCase"/>s.
    /// </summary>
    public class RunRequest
    {
        public string Id { get; set; }

        public string ClientName { get; set; }

        public string ClientVersion { get; set; }

        public TestCase[] Cases { get; set; }

    }
}
