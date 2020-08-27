using System;
using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    public class RunResult
    {
        [JsonProperty( Order = 1 )]
        public string Id { get; set; }

        [JsonProperty( Order = 2 )]
        public DateTime StartTime { get; set; }

        [JsonProperty( Order = 3 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 5 )]
        public TestState State { get; set; }

        [JsonProperty( Order = 10 )]
        public TestCase[] Cases { get; set; }

        [JsonProperty( Order = 20 )]
        public string Output { get; set; }
    }
}
