using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Communication
{
    public class ExploreRequest : BaseRequest
    {
        [JsonProperty( Order = 11 )]
        public string AssemblyPath { get; set; }
    }
}
