using System;
using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication
{
    public class Response
    {
        [JsonProperty( Order = 1 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 2 )]
        public string Id { get; set; }

        [JsonProperty( Order = 5 )]
        public string Directory { get; set; }
    }
}
