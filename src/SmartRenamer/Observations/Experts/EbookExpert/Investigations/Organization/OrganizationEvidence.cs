using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// =========================================================================
    /// OrganizationEvidence
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Preserves evidence discovered while researching how an ebook
    /// collection is organized.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Describe what was discovered.
    /// • Preserve supporting evidence.
    /// • Remain completely neutral.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Interpret results.
    /// • Produce recommendations.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to the Consultant.
    /// =========================================================================
    /// </summary>
    public class OrganizationEvidence
    {
        public string Category { get; set; } = "";

        public string Value { get; set; } = "";

        public List<string> Files { get; } = new();
    }
}