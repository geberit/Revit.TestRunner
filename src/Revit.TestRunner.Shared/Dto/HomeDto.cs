using Newtonsoft.Json;

namespace Revit.TestRunner.Shared.Dto
{
    /// <summary>
    /// Response dto, for a call on the main path.
    /// Contains information and paths to all functions.
    /// </summary>
    public class HomeDto : BaseResponseDto
    {
        public HomeDto() : base( DtoType.HomeDto )
        {
        }

        /// <summary>
        /// The logfile path.
        /// </summary>
        [JsonProperty( Order = 12 )]
        public string LogFilePath { get; set; }

        /// <summary>
        /// Current Revit version.
        /// </summary>
        [JsonProperty( Order = 15 )]
        public string RevitVersion { get; set; }

        /// <summary>
        /// Path for a request to explore an assembly.
        /// </summary>
        [JsonProperty( Order = 21 )]
        public string ExplorePath { get; set; }

        /// <summary>
        /// Path for a request to start a test run.
        /// </summary>
        [JsonProperty( Order = 22 )]
        public string TestPath { get; set; }
    }
}
