using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Dto for an assembly explore response.
    /// </summary>
    public class ExploreResponseDto : BaseResponseDto
    {
        public ExploreResponseDto() : base( DtoType.ExploreResponseDto )
        {
        }

        /// <summary>
        /// File path of the assembly requested.
        /// </summary>
        [JsonProperty( Order = 11 )]
        public string AssemblyPath { get; set; }

        /// <summary>
        /// File path of the NUnit explore xml file.
        /// </summary>
        [JsonProperty( Order = 12 )]
        public string ExploreFile { get; set; }

        /// <summary>
        /// Message.
        /// </summary>
        [JsonProperty( Order = 15 )]
        public string Message { get; set; }
    }
}
