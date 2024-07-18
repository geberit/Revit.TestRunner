using System;
using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Base dto for all responses.
    /// </summary>
    public abstract class BaseResponseDto
    {
        protected BaseResponseDto()
        {
            DtoType = DtoType.Unknown;
        }

        protected BaseResponseDto( DtoType dtoType )
        {
            DtoType = dtoType;
        }

        [JsonProperty( Order = 1 )]
        public DtoType DtoType { get; }

        [JsonProperty( Order = 2 )]
        public string RequestId { get; set; }

        [JsonProperty( Order = 3 )]
        public DateTime Timestamp { get; set; }

        [JsonProperty( Order = 100 )]
        public string ThisPath { get; set; }
    }
}
