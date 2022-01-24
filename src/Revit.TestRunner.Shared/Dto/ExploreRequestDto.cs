using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Dto for an assembly explore request.
    /// </summary>
    public class ExploreRequestDto : BaseRequestDto
    {
        public ExploreRequestDto() : base( DtoType.ExploreRequestDto )
        {
        }

        /// <summary>
        /// File path of the assembly to request.
        /// </summary>
        [JsonProperty( Order = 11 )]
        public string AssemblyPath { get; set; }
    }
}
