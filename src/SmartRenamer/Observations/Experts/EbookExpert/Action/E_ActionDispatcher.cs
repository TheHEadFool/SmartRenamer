using System;
using System.Collections.Generic;
using Scout.Observations.Conversation;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;

namespace SmartRenamer.Observations.Experts.EbookExpert.Action
{
    /// <summary>
    /// =========================================================================
    /// E_ActionDispatcher
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Routes generic Conversation Framework action requests to the appropriate
    /// Ebook Expert domain operation.
    ///
    /// The Conversation Framework knows:
    ///
    ///     "The user approved this action."
    ///
    /// The Ebook Expert knows:
    ///
    ///     "ResearchMissingIsbn means I should use the Repair Service."
    ///
    /// =========================================================================
    /// Responsibilities
    /// =========================================================================
    ///
    /// • Interpret Ebook-specific ActionIds.
    /// • Route actions to the appropriate Ebook domain service.
    /// • Preserve structured action results.
    /// • Return selectable options when an action produces candidates.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Interpret natural-language conversation.
    /// • Decide whether the user approved an action.
    /// • Modify EPUB files directly.
    /// • Perform ISBN research directly.
    /// • Select an ISBN candidate automatically.
    ///
    /// Those responsibilities belong to the Conversation Framework,
    /// E_RepairService, Resources, and User.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_ActionDispatcher
    {
        private readonly E_RepairService _repairService = new();

        /// <summary>
        /// Executes an Ebook Expert action against the supplied repair
        /// opportunities.
        /// </summary>
        public CV_ActionResult Execute(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (opportunities == null)
                throw new ArgumentNullException(nameof(opportunities));

            return request.ActionId switch
            {
                "ResearchMissingIsbn" =>
                    ResearchMissingIsbn(
                        request,
                        opportunities),

                _ =>
                    new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message =
                            $"The Ebook Expert does not recognize the action '{request.ActionId}'."
                    }
            };
        }

        /// <summary>
        /// Researches all currently discovered missing-ISBN opportunities.
        ///
        /// Research never modifies an EPUB.
        /// </summary>
        private CV_ActionResult ResearchMissingIsbn(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            List<string> evidence = new();
            List<CV_ActionOption> options = new();

            int researchedBooks = 0;
            int candidateCount = 0;

            foreach (RepairOpportunity opportunity in opportunities)
            {
                if (!opportunity.MissingIsbn)
                    continue;

                researchedBooks++;

                List<IsbnResearchCandidate> candidates =
                    _repairService.ResearchMissingIsbn(
                        opportunity);

                string fileName =
                    opportunity.Record?.File?.CurrentName
                    ?? "Unknown ebook";

                if (candidates.Count == 0)
                {
                    evidence.Add(
                        $"No ISBN candidates were found for {fileName}.");

                    continue;
                }

                foreach (IsbnResearchCandidate candidate in candidates)
                {
                    candidateCount++;

                    CV_ActionOption option = new()
                    {
                        // The candidate itself.
                        Id = candidate.Isbn,

                        // Identifies the specific ebook this candidate belongs to.
                        //
                        // OriginalFullPath is used rather than CurrentFullPath because
                        // the current name/path may change during the workflow.
                        ContextId =
         opportunity.Record.File?.OriginalFullPath
         ?? string.Empty,

                        Label =
         $"{candidate.Isbn} — {fileName}",

                        Confidence =
         candidate.Confidence,

                        Source =
         candidate.Source
                    };

                    option.Evidence.Add(
                        candidate.Evidence);

                    options.Add(option);
                }
            }

            if (researchedBooks == 0)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "No ebooks with missing ISBN information were available for research."
                };
            }

            CV_ActionResult result = new()
            {
                ActionId = request.ActionId,
                Success = true,
                Message =
        $"ISBN research completed for {researchedBooks} ebook(s). " +
        $"{candidateCount} candidate(s) were found."
            };

            result.Evidence.AddRange(evidence);
            result.Options.AddRange(options);

            return result;
        }
    }
}