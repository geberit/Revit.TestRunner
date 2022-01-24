using System;
using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Base dto for all requests.
    /// </summary>
    public abstract class BaseRequestDto
    {
        protected BaseRequestDto()
        {
            DtoType = DtoType.Unknown;
        }

        protected BaseRequestDto( DtoType dtoType )
        {
            DtoType = dtoType;
        }

        [JsonProperty( Order = 1 )]
        public DtoType DtoType { get; }

        [JsonProperty( Order = 2 )]
        public string RequestId { get; set; }

        [JsonProperty( Order = 3 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 8 )]
        public string ClientName { get; set; }

        [JsonProperty( Order = 9 )]
        public string ClientVersion { get; set; }
    }
}
