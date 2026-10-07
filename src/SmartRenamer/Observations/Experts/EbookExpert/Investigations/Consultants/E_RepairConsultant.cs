using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    /// <summary>
    /// =========================================================================
    /// E_RepairConsultant
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Reviews repair opportunities discovered by the Repair Block.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Interpret RepairReport.
    /// • Produce ExpertFindings.
    /// • Identify when additional research may be appropriate.
    /// • Never modify ebook files.
    ///
    /// This Consultant does NOT
    /// -------------------------------------------------------------------------
    /// • Repair metadata.
    /// • Read EPUB files.
    /// • Perform external research.
    /// • Communicate directly with Scout.
    ///
    /// Those responsibilities belong to the appropriate Ebook Expert
    /// Investigation or later repair capability.
    /// =========================================================================
    /// </summary>
    internal sealed class E_RepairConsultant
    {
        public List<ExpertFinding> Review(
            RepairReport report)
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Identity reconciliation
            //---------------------------------------------------------


            List<RepairOpportunity> identityOpportunities =
                report.Opportunities
                    .Where(opportunity => opportunity.IdentityEvaluation?.RepairRequired == true)
                    .ToList();

            foreach (RepairOpportunity opportunity in identityOpportunities)
            {
                string originalPath =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(originalPath))
                    continue;

                string fileName =
                    opportunity.Record?.File?.CurrentName
                    ?? opportunity.Record?.File?.OriginalName
                    ?? originalPath;

                ExpertFinding finding = new()
                {
                    FoundSomething = true,
                    ContextId = originalPath,
                    Summary =
                        $"Metadata identity or Series information needs reconciliation for {fileName}.",
                    Confidence = 1.0
                };

                finding.Evidence.Add(
                    $"Identity reconciliation required: {fileName}");

                if (opportunity.IdentityEvaluation != null)
                {
                    finding.Evidence.Add(
                        opportunity.IdentityEvaluation.Reason);

                    finding.Evidence.AddRange(
                        opportunity.IdentityEvaluation.Evidence);
                }

                finding.Questions.Add(
                    "The ebook's Title, Author, Series, or SeriesNumber information needs reconciliation before Scout can consider it complete.");

                findings.Add(finding);
            }

            //---------------------------------------------------------
            // Missing ISBN
            //---------------------------------------------------------


            if (report.MissingIsbns > 0)
            {
                ExpertFinding finding = new()
                {
                    FoundSomething = true,
                    Summary =
                        $"{EbookGrammar.CountNoun(report.MissingIsbns)} {EbookGrammar.IsAre(report.MissingIsbns)} missing ISBN information.",
                    Confidence = 1.0
                };

                finding.Evidence.Add(
                    $"Ebooks missing ISBN information: {report.MissingIsbns}");

                foreach (RepairOpportunity opportunity in report.Opportunities)
                {
                    if (!opportunity.MissingIsbn)
                        continue;

                    string fileName =
                        opportunity.Record.File.CurrentName;

                    finding.Evidence.Add(
                        $"ISBN missing: {fileName}");
                }

                finding.Questions.Add(
                    "Would you like Scout to research the missing ISBN information?");

                findings.Add(finding);
            }

            //---------------------------------------------------------
            // Unsupported repair capabilities
            //
            // These are real repair facts, but Scout does not yet have a
            // research/repair capability for them. Keep them tied to the
            // individual ebook so the Conversation layer can offer a
            // terminal human decision for that branch rather than leaving
            // the collection permanently blocked.
            //
            // ISBN and identity are intentionally excluded here because
            // they already have supported action paths above.
            //---------------------------------------------------------

            foreach (RepairOpportunity opportunity in report.Opportunities)
            {
                string originalPath =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(originalPath))
                    continue;

                // Title/Author may be resolved by the identity evaluator.
                // If identity reconciliation is already required, let that
                // supported path own the finding rather than offering a
                // premature unsupported-repair decision.
                bool identityHandled =
                    opportunity.IdentityEvaluation?.RepairRequired == true;

                List<string> unsupportedFields = new();

                if (opportunity.MissingTitle && !identityHandled)
                    unsupportedFields.Add("Title");

                if (opportunity.MissingAuthor && !identityHandled)
                    unsupportedFields.Add("Author");

                if (opportunity.MissingPublisher)
                    unsupportedFields.Add("Publisher");

                if (opportunity.MissingLanguage)
                    unsupportedFields.Add("Language");

                if (opportunity.MissingDescription)
                    unsupportedFields.Add("Description");

                if (opportunity.MissingCover)
                    unsupportedFields.Add("Cover");

                if (unsupportedFields.Count == 0)
                    continue;

                string fileName =
                    opportunity.Record?.File?.CurrentName
                    ?? opportunity.Record?.File?.OriginalName
                    ?? originalPath;

                ExpertFinding finding = new()
                {
                    FoundSomething = true,
                    ContextId = originalPath,
                    Summary =
                        $"{fileName} has repair needs that Scout cannot currently recover automatically.",
                    Confidence = 1.0
                };

                finding.Evidence.Add(
                    $"Missing or unresolved fields: {string.Join(", ", unsupportedFields)}.");

                finding.Evidence.Add(
                    "Scout currently has no supported research/repair capability for these fields." );

                finding.Questions.Add(
                    "Would you like to accept this ebook as-is, or omit it from Organization?" );

                findings.Add(finding);
            }

            return findings;
        }
        /// <summary>
        /// Creates the collection-level finding that exposes Scout's existing
        /// automatic-repair authorization capability. The finding does not
        /// claim that every opportunity is automatically repairable; the
        /// existing decision engine remains responsible for determining which
        /// repairs qualify when the user authorizes them.
        /// </summary>
        public ExpertFinding ReviewAutomaticRepairAuthorization(
            RepairReport report)
        {
            return new ExpertFinding
            {
                FoundSomething = true,
                Summary =
                    "Automatic ebook repair authorization is available.",
                Confidence = 1.0,
                Questions =
                {
                    "Would you like Scout to automatically handle ebook repairs that meet its existing safety rules?"
                },
                Evidence =
                {
                    $"{EbookGrammar.CountPhrase(report.Opportunities.Count, "repair opportunity", "repair opportunities")} " +
                    $"{EbookGrammar.IsAre(report.Opportunities.Count)} currently available.",
                    "Scout will still ask you when a repair is ambiguous or does not meet the automatic-repair rules."
                }
            };
        }

    }
}