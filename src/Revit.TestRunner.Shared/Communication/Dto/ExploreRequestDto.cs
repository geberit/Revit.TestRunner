using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication.Dto
{
    public class ExploreRequestDto : BaseRequestDto
    {
        public ExploreRequestDto() : base( DtoType.ExploreRequestDto )
        {
        }

        [JsonProperty( Order = 11 )]
        public string AssemblyPath { get; set; }
    }
}
