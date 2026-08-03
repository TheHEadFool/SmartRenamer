using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Data.Reports
{
    /// <summary>
    /// =========================================================================
    /// MetadataEvidence
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Preserves evidence discovered while analyzing ebook metadata.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Describe what was discovered.
    /// • Preserve the supporting evidence.
    /// • Remain neutral.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Decide significance.
    /// • Produce ExpertFindings.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Consultants.
    /// =========================================================================
    /// </summary>
    public class MetadataEvidence
    {
        public string Category { get; set; } = "";

        public string Value { get; set; } = "";

        public List<string> Files { get; } = new();
    }
}