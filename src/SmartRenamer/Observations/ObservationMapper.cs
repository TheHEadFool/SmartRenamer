using System;
using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// =========================================================================
    /// ObservationMapper
    /// =========================================================================
    ///
    /// Converts ExpertFindings into ProjectObservations that Scout's
    /// existing Workspace UI understands.
    ///
    /// The mapper is a compatibility bridge between the new Expert architecture
    /// and the existing UI model.
    ///
    /// =========================================================================
    /// IMPORTANT
    /// =========================================================================
    ///
    /// ExpertFindings remain the authoritative source of truth.
    ///
    /// This class does NOT:
    ///
    /// • Analyze files.
    /// • Create new findings.
    /// • Decide which Expert is most important.
    /// • Run Experts again.
    ///
    /// It only translates the Expert's findings into the model already
    /// understood by the Workspace UI.
    ///
    /// =========================================================================
    /// UI PRESENTATION RULE
    /// =========================================================================
    ///
    /// The left Observation panel should identify WHAT Scout discovered.
    ///
    /// The center Recommendation panel can then explain WHY Scout found it
    /// important and WHAT can be done about it.
    ///
    /// Therefore, specific findings such as:
    ///
    /// • Missing ISBNs
    /// • Missing Publishers
    /// • Incomplete Metadata
    /// • Missing Descriptions
    /// • Missing Series Information
    /// • Missing Covers
    ///
    /// should not all be collapsed into the generic "Improve Metadata"
    /// recommendation.
    ///
    /// Specific findings are tested before broader categories such as
    /// "metadata".
    ///
    /// =========================================================================
    /// FINDING ID PRESERVATION
    /// =========================================================================
    ///
    /// Each ProjectObservation preserves the identity of the
    /// ExpertFinding from which it was created.
    ///
    /// This allows the new Conversation Framework and the existing Workspace
    /// UI to refer to the same underlying discovery.
    ///
    ///     ExpertFinding
    ///          │
    ///          │ same Id
    ///          ▼
    ///     ProjectObservation
    ///
    /// The Review All system will use this shared identity to connect
    /// report links with the corresponding observation buttons.
    ///
    /// =========================================================================
    /// </summary>
    public static class ObservationMapper
    {
        /// <summary>
        /// Converts ExpertFindings into UI observations.
        ///
        /// Identical findings are combined so that the same discovery does
        /// not appear repeatedly in the Observation panel.
        /// </summary>
        public static List<ProjectObservation> Map(
            IEnumerable<ExpertFinding> findings)
        {
            List<ProjectObservation> observations = new();

            if (findings == null)
                return observations;

            //---------------------------------------------------------
            // Group identical findings.
            //
            // Multiple investigations may discover the same underlying
            // condition. The UI should present that condition once rather
            // than showing duplicate buttons.
            //---------------------------------------------------------

            IEnumerable<IGrouping<string, ExpertFinding>> groupedFindings =
                findings
                    .Where(f =>
                        f != null &&
                        f.FoundSomething &&
                        !string.IsNullOrWhiteSpace(f.Summary))
                    .GroupBy(
                        f => f.Summary.Trim(),
                        StringComparer.OrdinalIgnoreCase);

            //---------------------------------------------------------
            // Convert each distinct finding into a UI observation.
            //---------------------------------------------------------

            foreach (IGrouping<string, ExpertFinding> group in groupedFindings)
            {
                ExpertFinding primaryFinding =
                    group
                        .OrderByDescending(f => f.Confidence)
                        .First();

                //-----------------------------------------------------
                // Combine evidence from all findings in this group.
                //-----------------------------------------------------

                List<string> evidence =
                    group
                        .SelectMany(f => f.Evidence)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                //-----------------------------------------------------
                // Combine follow-up questions.
                //-----------------------------------------------------

                List<string> questions =
                    group
                        .SelectMany(f => f.Questions)
                        .Where(q => !string.IsNullOrWhiteSpace(q))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                //-----------------------------------------------------
                // Build the UI observation.
                //
                // IMPORTANT:
                //
                // Preserve the identity of the primary ExpertFinding.
                // Do NOT create a new Guid here.
                //
                //-----------------------------------------------------

                ProjectObservation observation = new()
                {
                    Id = primaryFinding.Id,

                    Title = primaryFinding.Summary,

                    Description =
                        evidence.Count > 0
                            ? string.Join(
                                Environment.NewLine,
                                evidence)
                            : primaryFinding.Summary,

                    WhyItMatters =
                        questions.Count > 0
                            ? string.Join(
                                Environment.NewLine,
                                questions)
                            : "Scout found something worth reviewing.",

                    Severity =
                        primaryFinding.Confidence >= 0.8
                            ? ObservationSeverity.Suggestion
                            : ObservationSeverity.Information,

                    Priority =
                        primaryFinding.Confidence >= 0.8
                            ? ObservationPriority.High
                            : ObservationPriority.Medium,

                    //-------------------------------------------------
                    // These properties are important.
                    //
                    // They give the existing UI something meaningful
                    // to display instead of an empty action.
                    //-------------------------------------------------

                    IsRecommended = true,

                    IsSelected = false,

                    ActionTitle = BuildActionTitle(
                        primaryFinding.Summary,
                        primaryFinding.Evidence),

                    ActionDescription =
                        BuildActionDescription(
                            primaryFinding,
                            questions)
                };

                observations.Add(observation);
            }

            return observations;
        }

        //---------------------------------------------------------
        // Action text
        //---------------------------------------------------------

        /// <summary>
        /// Builds the short label displayed by the Observation button.
        ///
        /// Specific findings are deliberately checked before general
        /// categories. This prevents findings such as missing ISBNs or
        /// missing publishers from being incorrectly labeled simply as
        /// "Improve Metadata."
        ///
        /// The button identifies the discovery. The center panel provides
        /// the fuller explanation and the conversation can provide further
        /// discussion.
        /// </summary>
        private static string BuildActionTitle(
            string summary,
            IReadOnlyList<string> evidence)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return "Explore Finding";

            string lower =
                summary.Trim().ToLowerInvariant();

            //---------------------------------------------------------
            // Specific cover finding.
            //---------------------------------------------------------

            if (lower.Contains("missing cover"))
                return "Add Missing Covers";

            //---------------------------------------------------------
            // Specific series finding.
            //---------------------------------------------------------

            if (lower.Contains("missing series"))
                return "Add Series Information";

            //---------------------------------------------------------
            // Specific description finding.
            //---------------------------------------------------------

            if (lower.Contains("missing description"))
                return "Add Missing Descriptions";

            //---------------------------------------------------------
            // Specific ISBN findings.
            //
            // This must come before the general metadata test.
            //---------------------------------------------------------

            if (lower.Contains("missing isbn"))
                return "Missing ISBNs";

            if (lower.Contains("no isbn"))
                return "Missing ISBNs";

            if (lower.Contains("duplicate isbn"))
                return "Duplicate ISBNs";

            if (lower.Contains("sharing duplicate isbn"))
                return "Duplicate ISBNs";

            //---------------------------------------------------------
            // Specific publisher findings.
            //
            // This must come before the general metadata test.
            //---------------------------------------------------------

            if (lower.Contains("missing publisher"))
                return "Missing Publishers";

            if (lower.Contains("no publisher"))
                return "Missing Publishers";

            //---------------------------------------------------------
            // Specific incomplete-metadata finding.
            //---------------------------------------------------------

            if (lower.Contains("incomplete metadata"))
                return "Incomplete Metadata";

            //---------------------------------------------------------
            // Enrichment is more specific than the general metadata
            // category.
            //
            // "could be enriched with additional metadata" contains
            // the word "metadata", so enrichment must be tested first.
            //---------------------------------------------------------

            if (lower.Contains("enrich"))
                return "Enrich Metadata";

            //---------------------------------------------------------
            // General metadata improvement.
            //
            // This remains as a safeguard for metadata findings that
            // do not have a more specific category.
            //---------------------------------------------------------

            if (lower.Contains("metadata"))
                return "Improve Metadata";

            //---------------------------------------------------------
            // Organization findings.
            //---------------------------------------------------------

            if (lower.Contains("publisher"))
                return "Multiple Publishers";

            if (lower.Contains("language"))
                return "Multiple Languages";

            if (lower.Contains("related"))
                return "Organize Related Files";

            if (lower.Contains("library"))
                return "Organize Library";

            if (lower.Contains("project"))
                return "Organize Project";

            //---------------------------------------------------------
            // Safeguard.
            //
            // Never invent an action for an observation we do not
            // understand yet.
            //---------------------------------------------------------

            return "Explore Finding";
        }

        /// <summary>
        /// Builds the description associated with an observation.
        ///
        /// Follow-up questions are preserved so the existing UI and
        /// conversation layer can continue to use the Expert's questions.
        /// </summary>
        private static string BuildActionDescription(
            ExpertFinding finding,
            IReadOnlyList<string> questions)
        {
            if (questions != null && questions.Count > 0)
            {
                return string.Join(
                    Environment.NewLine,
                    questions);
            }

            if (!string.IsNullOrWhiteSpace(finding.Summary))
            {
                return
                    $"Scout can help you review this finding: {finding.Summary}";
            }

            return "Explore this observation with Scout.";
        }
    }
}