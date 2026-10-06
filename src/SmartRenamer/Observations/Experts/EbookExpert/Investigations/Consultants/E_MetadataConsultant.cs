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
                        $"{EbookGrammar.CountNoun(report.NeedsAttention)} {EbookGrammar.HasHave(report.NeedsAttention)} very incomplete metadata.",
                        $"{EbookGrammar.CountNoun(report.NeedsAttention)} {EbookGrammar.WasWere(report.NeedsAttention)} identified by the metadata analysis as needing significant metadata attention.",
                        "Would you like Scout to review the ebooks with the most incomplete metadata first?"));
            }

            if (report.IncompleteMetadata > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.IncompleteMetadata)} {EbookGrammar.HasHave(report.IncompleteMetadata)} incomplete metadata.",
                        $"{EbookGrammar.CountNoun(report.IncompleteMetadata)} {EbookGrammar.WasWere(report.IncompleteMetadata)} identified as having incomplete metadata.",
                        "Would you like Scout to review which metadata fields are missing?"));
            }

            //---------------------------------------------------------
            // Missing identity metadata
            //---------------------------------------------------------

            if (report.MissingTitles > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.MissingTitles)} {EbookGrammar.IsAre(report.MissingTitles)} missing title metadata.",
                        $"{EbookGrammar.CountNoun(report.MissingTitles)} {EbookGrammar.WasWere(report.MissingTitles)} identified as having no title metadata.",
                        "Would you like Scout to help identify the missing titles?"));
            }

            if (report.MissingAuthors > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.MissingAuthors)} {EbookGrammar.IsAre(report.MissingAuthors)} missing author metadata.",
                        $"{EbookGrammar.CountNoun(report.MissingAuthors)} {EbookGrammar.WasWere(report.MissingAuthors)} identified as having no author metadata.",
                        "Would you like Scout to help identify the missing authors?"));
            }

            if (report.MissingIsbns > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.MissingIsbns)} {EbookGrammar.IsAre(report.MissingIsbns)} missing ISBN metadata.",
                        $"{EbookGrammar.CountNoun(report.MissingIsbns)} {EbookGrammar.WasWere(report.MissingIsbns)} identified as having no ISBN metadata.",
                        "Would you like Scout to review the ebooks missing ISBN information?"));
            }

            //---------------------------------------------------------
            // Supporting metadata
            //---------------------------------------------------------

            if (report.MissingPublishers > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.MissingPublishers)} {EbookGrammar.IsAre(report.MissingPublishers)} missing publisher metadata.",
                        $"{EbookGrammar.CountNoun(report.MissingPublishers)} {EbookGrammar.WasWere(report.MissingPublishers)} identified as having no publisher metadata.",
                        "Would you like Scout to review the missing publisher information?"));
            }

            if (report.MissingLanguages > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.MissingLanguages)} {EbookGrammar.IsAre(report.MissingLanguages)} missing language metadata.",
                        $"{EbookGrammar.CountNoun(report.MissingLanguages)} {EbookGrammar.WasWere(report.MissingLanguages)} identified as having no language metadata.",
                        "Would you like Scout to review the missing language information?"));
            }

            if (report.MissingDescriptions > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.MissingDescriptions)} {EbookGrammar.IsAre(report.MissingDescriptions)} missing descriptions.",
                        $"{EbookGrammar.CountNoun(report.MissingDescriptions)} {EbookGrammar.WasWere(report.MissingDescriptions)} identified as having no description metadata.",
                        "Would you like Scout to review the ebooks missing descriptions?"));
            }

            //---------------------------------------------------------
            // Covers
            //---------------------------------------------------------

            if (report.MissingCovers > 0)
            {
                findings.Add(
                    CreateFinding(
                        $"{EbookGrammar.CountNoun(report.MissingCovers)} {EbookGrammar.IsAre(report.MissingCovers)} missing cover images.",
                        $"{EbookGrammar.CountNoun(report.MissingCovers)} {EbookGrammar.WasWere(report.MissingCovers)} identified as having no cover image.",
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
                        $"{EbookGrammar.CountNoun(report.ExcellentMetadata)} {EbookGrammar.HasHave(report.ExcellentMetadata)} excellent metadata coverage.",
                        $"{EbookGrammar.CountNoun(report.ExcellentMetadata)} {EbookGrammar.WasWere(report.ExcellentMetadata)} identified as having excellent metadata coverage.",
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