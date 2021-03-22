using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication.Dto
{
    public class HomeDto : BaseResponseDto
    {
        public HomeDto() : base( DtoType.HomeDto )
        {
        }

        [JsonProperty( Order = 12 )]
        public string LogFilePath { get; set; }

        [JsonProperty( Order = 15 )]
        public string RevitVersion { get; set; }

        [JsonProperty( Order = 21 )]
        public string ExplorePath { get; set; }

        [JsonProperty( Order = 22 )]
        public string TestPath { get; set; }
    }
}
