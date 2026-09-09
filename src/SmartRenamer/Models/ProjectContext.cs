using System.Collections.Generic;
using SmartRenamer.Models.Analysis;
using SmartRenamer.Services;

namespace SmartRenamer.Models
{
    public class ProjectContext
    {
        public FolderSummary? Folder { get; set; }

        /// <summary>
        /// Every file participating in the current Scout analysis.
        /// This allows Observation Experts to inspect the actual files
        /// without needing to know where they originated.
        /// </summary>

        public string ProjectGoal { get; set; } = "";

        /// <summary>
        /// Scout protects the user's source collection.
        ///
        /// Repair and organization operate on working copies or create
        /// separate destination representations rather than modifying
        /// the source originals.
        /// </summary>
        public bool KeepOriginals { get; set; } = true;

        public string ProjectType { get; set; } = "Unknown";

        public int Confidence { get; set; }

        /// <summary>
        /// Collection-level organization choices for this project.
        ///
        /// These settings remain with the project so that organization
        /// planning can be performed incrementally as individual files
        /// complete their investigations and repairs.
        /// </summary>
        public OrganizationOptions Organization { get; set; } = new();

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