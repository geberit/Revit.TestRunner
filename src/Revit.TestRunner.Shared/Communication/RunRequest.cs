using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication
{
    /// <summary>
    /// Represents a test run, containing a set of <see cref="TestCase"/>s.
    /// </summary>
    public class RunRequest : BaseRequest
    {
        [JsonProperty( Order = 11 )]
        public TestCase[] Cases { get; set; }
    }
}
