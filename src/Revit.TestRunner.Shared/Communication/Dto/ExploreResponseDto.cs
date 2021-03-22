using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication.Dto
{
    public class ExploreResponseDto : BaseResponseDto
    {
        public ExploreResponseDto() : base( DtoType.ExploreResponseDto )
        {
        }

        [JsonProperty( Order = 11 )]
        public string AssemblyPath { get; set; }

        [JsonProperty( Order = 12 )]
        public string ExploreFile { get; set; }

        [JsonProperty( Order = 15 )]
        public string Message { get; set; }
    }
}
