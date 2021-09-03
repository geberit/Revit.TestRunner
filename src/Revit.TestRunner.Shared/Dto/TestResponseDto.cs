using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Response dto for a test run request.
    /// Conains information about the run.
    /// </summary>
    public class TestResponseDto : BaseResponseDto
    {
        public TestResponseDto() : base( DtoType.TestResponseDto )
        {
        }

        [JsonProperty( Order = 11 )]
        public string ResponseDirectory { get; set; }

        [JsonProperty( Order = 12 )]
        public string ResultFile { get; set; }

        [JsonProperty( Order = 13 )]
        public string ResultXmlFile { get; set; }

        [JsonProperty( Order = 14 )]
        public string SummaryFile { get; set; }
    }
}