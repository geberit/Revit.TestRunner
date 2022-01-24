using System;
using Newtonsoft.Json;
using Revit.TestRunner.Shared.Communication;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Represents the current state of a test run.
    /// </summary>
    public class TestRunStateDto
    {
        [JsonProperty( Order = 1 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 2 )]
        public string Id { get; set; }

        [JsonProperty( Order = 5 )]
        public DateTime StartTime { get; set; }

        [JsonProperty( Order = 6 )]
        public DateTime EndTime { get; set; }

        [JsonProperty( Order = 7 )]
        public string Output { get; set; }

        [JsonProperty( Order = 8 )]
        public string SummaryFile { get; set; }

        [JsonProperty( Order = 9 )]
        public TestState State { get; set; }

        [JsonProperty( Order = 10 )]
        public TestCaseDto[] Cases { get; set; }

        
    }
}
