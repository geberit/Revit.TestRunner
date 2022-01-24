using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Dto for a test run request.
    /// </summary>
    public class TestRequestDto : BaseRequestDto
    {
        public TestRequestDto() : base( DtoType.TestRequestDto )
        {
        }

        /// <summary>
        /// All tests to execute.
        /// </summary>
        [JsonProperty( Order = 11 )]
        public TestCaseDto[] Cases { get; set; }
    }
}
