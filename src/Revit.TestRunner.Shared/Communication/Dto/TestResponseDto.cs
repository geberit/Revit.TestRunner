using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication.Dto
{
    public class TestResponseDto : BaseResponseDto
    {
        public TestResponseDto() : base( DtoType.TestResponseDto )
        {
        }

        [JsonProperty( Order = 11 )]
        public string ResponseDirectory { get; set; }
    }
}
