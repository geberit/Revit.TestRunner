using System;
using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication
{
    public abstract class BaseRequest
    {
        [JsonProperty( Order = 1 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 2 )]
        public string Id { get; set; }

        [JsonProperty( Order = 5 )]
        public string ClientName { get; set; }

        [JsonProperty( Order = 6 )]
        public string ClientVersion { get; set; }
    }
}
