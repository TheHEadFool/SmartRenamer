using System.Collections.Generic;

namespace SmartRenamer.Models.Analysis
{
    public class ProjectProfile
    {
        public string ProjectType { get; set; } = "Unknown";

        public int Confidence { get; set; }

        public ProjectStatistics Statistics { get; set; } = new();

        public List<ProjectCollection> Collections { get; } = new();

        public List<DetectedDevice> Devices { get; } = new();

        public List<MetadataCapability> Metadata { get; } = new();

        public List<Recommendation> Recommendations { get; } = new();
    }
}