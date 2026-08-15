using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    /// <summary>
    /// =========================================================================
    /// E_MetadataConsultant
    /// =========================================================================
    ///
    /// PURPOSE
    /// -------------------------------------------------------------------------
    /// Interprets the MetadataReport produced by E_MetadataBlock and converts
    /// objective metadata observations into complete ExpertFindings.
    ///
    /// RESPONSIBILITIES
    /// -------------------------------------------------------------------------
    /// • Interpret metadata research.
    /// • Identify missing metadata.
    /// • Identify incomplete metadata.
    /// • Identify metadata consistency observations.
    /// • Provide supporting evidence.
    /// • Provide follow-up questions.
    /// • Provide confidence for deterministic metadata observations.
    ///
    /// DOES NOT
    /// -------------------------------------------------------------------------
    /// • Read ebook files.
    /// • Acquire metadata.
    /// • Modify metadata.
    /// • Repair ebooks.
    /// • Communicate with Scout.
    ///
    /// The Block acquires the facts.
    /// The Report preserves the facts.
    /// This Consultant interprets those facts.
    ///
    /// ARCHITECTURE
    /// -------------------------------------------------------------------------
    ///
    ///     Metadata Block
    ///          ↓
    ///     MetadataReport
    ///          ↓
    ///     E_MetadataConsultant
    ///          ↓
    ///     ExpertFinding
    ///
    /// The resulting MetadataReport remains available to downstream
    /// Investigations so metadata is acquired only once.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_MetadataConsultant
    {
        /// <summary>
        /// Reviews the MetadataReport and produces complete metadata findings.
        /// </summary>
        public List<ExpertFinding> Review(
            MetadataReport report)
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Collection
            //---------------------------------------------------------

            if (report.EpubFiles == 0)
            {
                return findings;
            }

            //---------------------------------------------------------
            // Metadata completeness
            //---------------------------------------------------------

            if (report.NeedsAttention > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.NeedsAttention} ebooks have very incomplete metadata.",
                        $"{report.NeedsAttention} ebooks were identified by the metadata analysis as needing significant metadata attention.",
                        "Would you like Scout to review the ebooks with the most incomplete metadata first?"));
            }

            if (report.IncompleteMetadata > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.IncompleteMetadata} ebooks have incomplete metadata.",
                        $"{report.IncompleteMetadata} ebooks were identified as having incomplete metadata.",
                        "Would you like Scout to review which metadata fields are missing?"));
            }

            //---------------------------------------------------------
            // Missing identity metadata
            //---------------------------------------------------------

            if (report.MissingTitles > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.MissingTitles} ebooks are missing title metadata.",
                        $"{report.MissingTitles} ebooks were identified as having no title metadata.",
                        "Would you like Scout to help identify the missing titles?"));
            }

            if (report.MissingAuthors > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.MissingAuthors} ebooks are missing author metadata.",
                        $"{report.MissingAuthors} ebooks were identified as having no author metadata.",
                        "Would you like Scout to help identify the missing authors?"));
            }

            if (report.MissingIsbns > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.MissingIsbns} ebooks are missing ISBN metadata.",
                        $"{report.MissingIsbns} ebooks were identified as having no ISBN metadata.",
                        "Would you like Scout to review the ebooks missing ISBN information?"));
            }

            //---------------------------------------------------------
            // Supporting metadata
            //---------------------------------------------------------

            if (report.MissingPublishers > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.MissingPublishers} ebooks are missing publisher metadata.",
                        $"{report.MissingPublishers} ebooks were identified as having no publisher metadata.",
                        "Would you like Scout to review the missing publisher information?"));
            }

            if (report.MissingLanguages > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.MissingLanguages} ebooks are missing language metadata.",
                        $"{report.MissingLanguages} ebooks were identified as having no language metadata.",
                        "Would you like Scout to review the missing language information?"));
            }

            if (report.MissingDescriptions > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.MissingDescriptions} ebooks are missing descriptions.",
                        $"{report.MissingDescriptions} ebooks were identified as having no description metadata.",
                        "Would you like Scout to review the ebooks missing descriptions?"));
            }

            //---------------------------------------------------------
            // Covers
            //---------------------------------------------------------

            if (report.MissingCovers > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.MissingCovers} ebooks are missing cover images.",
                        $"{report.MissingCovers} ebooks were identified as having no cover image.",
                        "Would you like Scout to review the ebooks missing cover images?"));
            }

            //---------------------------------------------------------
            // Consistency
            //---------------------------------------------------------

            if (report.DuplicateIsbns > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"The library contains {report.DuplicateIsbns} duplicate ISBN groups.",
                        $"{report.DuplicateIsbns} groups of ebooks were identified as sharing duplicate ISBN values.",
                        "Would you like Scout to review the duplicate ISBN groups?"));
            }

            if (report.DuplicateTitles > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"The library contains {report.DuplicateTitles} duplicate title groups.",
                        $"{report.DuplicateTitles} groups of ebooks were identified as sharing duplicate title values.",
                        "Would you like Scout to review the duplicate title groups?"));
            }

            //---------------------------------------------------------
            // Positive observation
            //---------------------------------------------------------

            if (report.ExcellentMetadata > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{report.ExcellentMetadata} ebooks have excellent metadata coverage.",
                        $"{report.ExcellentMetadata} ebooks were identified as having excellent metadata coverage.",
                        "Would you like Scout to leave these ebooks unchanged and focus on the ones needing attention?"));
            }

            return findings;
        }

        /// <summary>
        /// Creates a complete ExpertFinding from a deterministic metadata
        /// observation.
        ///
        /// Confidence is 1.0 because the Consultant is reporting a condition
        /// established by the MetadataReport itself, rather than estimating
        /// whether the condition exists.
        /// </summary>
        private static ExpertFinding CreateFinding(
            string summary,
            string evidence,
            string question)
        {
            ExpertFinding finding = new()
            {
                FoundSomething = true,
                Summary = summary,
                Confidence = 1.0
            };

            finding.Evidence.Add(evidence);
            finding.Questions.Add(question);

            return finding;
        }
    }
}