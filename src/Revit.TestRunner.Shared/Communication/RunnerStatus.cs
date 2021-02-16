using System;
using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication
{
    public class RunnerStatus
    {
        [JsonProperty( Order = 1 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 2 )]
        public string LogFilePath { get; set; }

        [JsonProperty( Order = 5 )]
        public string RevitVersion { get; set; }
    }
}
