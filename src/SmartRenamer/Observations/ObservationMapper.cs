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
                //-----------------------------------------------------

                ProjectObservation observation = new()
                {
                    Id = Guid.NewGuid(),

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

        private static string BuildActionTitle(
    string summary,
    IReadOnlyList<string> evidence)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return "Explore Finding";

            string lower = summary.Trim().ToLowerInvariant();

            // ---------------------------------------------------------
            // Specific findings first.
            //
            // More specific tests must come before broader tests such
            // as "metadata".
            // ---------------------------------------------------------

            if (lower.Contains("missing cover"))
                return "Add Missing Covers";

            if (lower.Contains("missing series"))
                return "Add Series Information";

            if (lower.Contains("missing description"))
                return "Add Missing Descriptions";

            if (lower.Contains("enrich"))
                return "Enrich Metadata";

            // ---------------------------------------------------------
            // General metadata improvement.
            // ---------------------------------------------------------

            if (lower.Contains("metadata"))
                return "Improve Metadata";

            // ---------------------------------------------------------
            // Organization findings.
            // ---------------------------------------------------------

            if (lower.Contains("related"))
                return "Organize Related Files";

            if (lower.Contains("library"))
                return "Organize Library";

            if (lower.Contains("project"))
                return "Organize Project";

            // ---------------------------------------------------------
            // Safeguard.
            //
            // Never invent an action for an observation we do not
            // understand yet.
            // ---------------------------------------------------------

            return "Explore Finding";
        }
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