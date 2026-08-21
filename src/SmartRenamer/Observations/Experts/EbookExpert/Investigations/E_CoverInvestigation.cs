using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations;

/// <summary>
/// =========================================================================
/// E_CoverInvestigation
/// =========================================================================
///
/// Purpose
/// -------------------------------------------------------------------------
/// Coordinates cover-related investigation for the Ebook Expert.
///
/// The metadata investigation already determines which ebooks have covers.
/// This investigation interprets those facts through the Cover Consultant.
///
/// Responsibilities
/// -------------------------------------------------------------------------
/// • Review cover availability.
/// • Identify ebooks with missing covers.
/// • Produce cover-related ExpertFindings.
/// • Preserve the existing MetadataReport as the source of facts.
///
/// This Investigation does NOT
/// -------------------------------------------------------------------------
/// • Read ebook files directly.
/// • Modify ebook files.
/// • Download covers.
/// • Select replacement artwork.
///
/// Those responsibilities belong to the appropriate future Cover
/// Specialists/Blocks and action pipeline.
///
/// =========================================================================
/// </summary>
public sealed class E_CoverInvestigation
{
    /// <summary>
    /// Investigates cover availability using the metadata already acquired
    /// by the Ebook Expert.
    /// </summary>
    public List<ExpertFinding> Investigate(
        MetadataReport metadataReport)
    {
        List<ExpertFinding> findings = new();

        //---------------------------------------------------------
        // Ask the Cover Consultant to interpret the facts.
        //---------------------------------------------------------

        E_CoverConsultant consultant = new();

        findings.AddRange(
            consultant.Review(metadataReport));

        return findings;
    }
}