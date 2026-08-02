using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Blocks
{
    /// <summary>
    /// =========================================================================
    /// E_MetadataBlock
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Understand the metadata contained within an ebook.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Read ebook metadata.
    /// • Interpret metadata.
    /// • Validate metadata.
    /// • Detect metadata problems.
    /// • Produce a MetadataReport.
    ///
    /// This Block does NOT
    /// -------------------------------------------------------------------------
    /// • Produce ExpertFindings.
    /// • Communicate with Scout.
    /// • Decide what is important.
    ///
    /// Those responsibilities belong to Consultants.
    /// =========================================================================
    /// </summary>
    public class E_MetadataBlock
    {
        public MetadataReport Analyze(
     IReadOnlyList<FileContext> files)
        {
            return new MetadataReport();
        }
    }
}