using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication.Dto
{
    public class TestRequestDto : BaseRequestDto
    {
        public TestRequestDto() : base( DtoType.TestRequestDto )
        {
        }

        [JsonProperty( Order = 11 )]
        public TestCaseDto[] Cases { get; set; }
    }
}
