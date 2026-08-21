using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;

/// <summary>
/// =========================================================================
/// E_CoverConsultant
/// =========================================================================
///
/// Purpose
/// -------------------------------------------------------------------------
/// Interprets cover availability discovered by the Metadata investigation.
///
/// The Metadata investigation acquires the facts.
/// This Consultant interprets those facts and creates an ExpertFinding.
///
/// Responsibilities
/// -------------------------------------------------------------------------
/// • Interpret cover-related information from the MetadataReport.
/// • Identify ebooks with missing cover images.
/// • Produce an ExpertFinding describing the cover condition.
/// • Provide evidence supporting the finding.
/// • Provide a question that can guide a future conversation.
///
/// This Consultant does NOT
/// -------------------------------------------------------------------------
/// • Read ebook files directly.
/// • Modify ebook files.
/// • Download cover images.
/// • Select replacement artwork.
/// • Decide how the finding is ultimately presented to the user.
///
/// Those responsibilities belong to the appropriate Investigation,
/// Specialists, action pipeline, and Recommendation Translator.
///
/// Architecture
/// -------------------------------------------------------------------------
/// Metadata Investigation
///         ↓
/// MetadataReport
///         ↓
/// E_CoverInvestigation
///         ↓
/// E_CoverConsultant
///         ↓
/// ExpertFinding
///         ↓
/// Recommendation Translator
///         ↓
/// Scout
///
/// Design Principle
/// -------------------------------------------------------------------------
/// Consultants interpret domain-specific reports and turn them into
/// meaningful ExpertFindings. They should provide facts, evidence, and
/// useful questions without taking over the responsibilities of the
/// recommendation or conversation layers.
///
/// =========================================================================
/// </summary>
internal sealed class E_CoverConsultant
{
    /// <summary>
    /// Reviews cover information and produces cover-related findings.
    /// </summary>
    /// <param name="report">
    /// The MetadataReport produced by the Metadata Investigation.
    /// </param>
    /// <returns>
    /// A list of ExpertFindings describing cover-related conditions.
    /// </returns>
    public List<ExpertFinding> Review(
        MetadataReport report)
    {
        List<ExpertFinding> findings = new();

        //---------------------------------------------------------
        // Domain applicability
        //---------------------------------------------------------
        // If there are no EPUB files, there is nothing for the
        // Cover Consultant to investigate.
        //---------------------------------------------------------

        if (report.EpubFiles == 0)
            return findings;

        //---------------------------------------------------------
        // Missing covers
        //---------------------------------------------------------
        // The Metadata Investigation has already determined the
        // number of ebooks without cover images.
        //
        // The Consultant interprets that information rather than
        // performing another file-level scan.
        //---------------------------------------------------------

        if (report.MissingCovers > 0)
        {
            ExpertFinding finding = new()
            {
                FoundSomething = true,

                Summary =
                    $"{report.MissingCovers} ebooks are missing cover images.",

                Confidence = 1.0
            };

            //---------------------------------------------------------
            // Evidence
            //---------------------------------------------------------
            // Evidence should support the finding without simply
            // repeating the Summary in several different forms.
            //---------------------------------------------------------

            finding.Evidence.Add(
                "Cover availability was determined from the ebook metadata.");

            //---------------------------------------------------------
            // Conversation question
            //---------------------------------------------------------
            // This question gives the Conversation Framework a
            // meaningful next step without requiring the Consultant
            // to control the conversation itself.
            //---------------------------------------------------------

            finding.Questions.Add(
                "Would you like me to show you the ebooks that are missing covers?");

            findings.Add(finding);
        }

        return findings;
    }
}