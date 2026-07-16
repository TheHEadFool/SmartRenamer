using System.Collections.Generic;
using SmartRenamer.Models.Analysis;
using SmartRenamer.Services;

namespace SmartRenamer.Models
{
    public class ProjectContext
    {
        public FolderSummary? Folder { get; set; }

        public string ProjectGoal { get; set; } = "";

        public bool KeepOriginals { get; set; } = true;

        public string ProjectType { get; set; } = "Unknown";

        public int Confidence { get; set; }

        // Complete analysis profile
        public ProjectProfile Profile { get; set; } = new();

        // Human-readable observations
        public List<ProjectObservation> Observations { get; }
            = new();

        // Older UI compatibility
        public List<string> RecommendedCapabilities { get; }
            = new();

        // Scout recommendations
        public List<Recommendation> Recommendations { get; }
            = new();
    }
}