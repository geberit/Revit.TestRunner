using System;
using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication
{
    public class ExploreResponse
    {
        [JsonProperty( Order = 1 )]
        public string Id { get; set; }

        [JsonProperty( Order = 3 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 5 )]
        public string AssemblyPath { get; set; }

        [JsonProperty( Order = 7 )]
        public string ExploreFile { get; set; }

        [JsonProperty( Order = 8 )]
        public string Message { get; set; }
    }
}
