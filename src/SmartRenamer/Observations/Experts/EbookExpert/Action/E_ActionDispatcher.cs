using Scout.Observations.Conversation;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;
using System;
using System.Collections.Generic;

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
    /// -------------------------------------------------------------------------
    /// • Interpret Ebook-specific ActionIds.
    /// • Route actions to the appropriate Ebook domain service.
    /// • Preserve structured action results.
    /// • Return selectable options when an action produces candidates.
    /// • Preserve user-approved selections for later Ebook operations.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Interpret natural-language conversation.
    /// • Modify EPUB files directly.
    /// • Perform ISBN research directly.
    /// • Select an ISBN candidate automatically.
    ///
    /// The user must explicitly select the candidate.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_ActionDispatcher
    {
        private readonly E_RepairService _repairService = new();

        //---------------------------------------------------------
        // Approved ISBN selections
        //---------------------------------------------------------
        //
        // Key:
        //     Original ebook path
        //
        // Value:
        //     ISBN explicitly selected by the user
        //
        // This state belongs to the Ebook Expert action layer.
        // The UI and Conversation Framework remain domain-neutral.
        //
        //---------------------------------------------------------

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

            //---------------------------------------------------------
            // A request containing OptionId represents a specific
            // candidate selected by the user.
            //
            // Research requests do not contain an OptionId.
            //---------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(request.OptionId) &&
    string.Equals(
        request.ActionId,
        "ResearchMissingIsbn",
        StringComparison.OrdinalIgnoreCase))
            {
                return SelectIsbnCandidate(
                    request,
                    opportunities);
            }


            return request.ActionId switch
            {
                "ResearchMissingIsbn" =>
                    ResearchMissingIsbn(
                        request,
                        opportunities),

                "ExecuteRepairPlan" =>
                    ExecuteRepairPlan(
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
        /// Executes all approved repairs for the EPUB identified by ContextId.
        ///
        /// The Repair Service creates one working copy, applies the complete
        /// approved repair plan, verifies the result, and preserves the
        /// completed repaired EPUB for the later output stage.
        /// </summary>
        private CV_ActionResult ExecuteRepairPlan(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            if (string.IsNullOrWhiteSpace(request.ContextId))
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I received the repair request, but I don't know which ebook it belongs to."
                };
            }

            RepairOpportunity? selectedOpportunity = null;

            foreach (RepairOpportunity opportunity in opportunities)
            {
                string originalPath =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                if (string.Equals(
                    originalPath,
                    request.ContextId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    selectedOpportunity = opportunity;
                    break;
                }
            }

            if (selectedOpportunity == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I couldn't match the repair request to the ebook in the current investigation."
                };
            }

            string? repairedPath =
                _repairService.ExecuteRepairPlan(
                    selectedOpportunity);

            if (string.IsNullOrWhiteSpace(repairedPath))
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I couldn't complete the approved repair plan for this ebook."
                };
            }

            CV_ActionResult result = new()
            {
                ActionId = request.ActionId,
                Success = true,
                Message =
        "The approved repair has been applied. " +
        "The repaired EPUB is ready for the next investigation pass."
            };

            result.Evidence.Add(
                $"Completed repaired EPUB: {repairedPath}");

            return result;

        }
        /// <summary>
        /// Records an ISBN candidate explicitly selected by the user.
        ///
        /// The selected ISBN is validated against the candidates produced
        /// by the Ebook Expert's research operation.
        ///
        /// No EPUB is modified here.
        /// </summary>
        private CV_ActionResult SelectIsbnCandidate(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            if (string.IsNullOrWhiteSpace(request.ContextId))
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I received the ISBN selection, but I don't know which ebook it belongs to."
                };
            }

            //---------------------------------------------------------
            // Find the specific ebook identified by ContextId.
            //---------------------------------------------------------

            RepairOpportunity? selectedOpportunity = null;

            foreach (RepairOpportunity opportunity in opportunities)
            {
                string originalPath =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                if (string.Equals(
                    originalPath,
                    request.ContextId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    selectedOpportunity = opportunity;
                    break;
                }
            }

            if (selectedOpportunity == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I couldn't match that ISBN selection to an ebook in the current investigation."
                };
            }

            //---------------------------------------------------------
            // Research the candidates for this specific ebook.
            //
            // This is validation only. No file is modified.
            //---------------------------------------------------------

            List<IsbnResearchCandidate> candidates =
                _repairService.ResearchMissingIsbn(
                    selectedOpportunity);

            IsbnResearchCandidate? selectedCandidate = null;

            foreach (IsbnResearchCandidate candidate in candidates)
            {
                if (string.Equals(
                    candidate.Isbn,
                    request.OptionId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    selectedCandidate = candidate;
                    break;
                }
            }

            if (selectedCandidate == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        $"I couldn't verify ISBN '{request.OptionId}' as a researched candidate for this ebook."
                };
            }

            IsbnResearchCandidate approvedCandidate =
    selectedCandidate;

            //---------------------------------------------------------
            // The user explicitly selected this ISBN.
            //---------------------------------------------------------

            string approvedIsbn =
    approvedCandidate.Isbn;



            //---------------------------------------------------------
            // The original EPUB is not modified here.
            //
            // The approved ISBN is recorded in the repair plan.
            // The actual repair occurs later when the repair plan
            // is explicitly executed.
            //---------------------------------------------------------

            //---------------------------------------------------------
            // Record the user's explicit approval.
            //
            // The repair is NOT physically performed here.
            // It is added to the Ebook Expert's repair plan.
            //---------------------------------------------------------

            _repairService.AddRepairChange(
                request.ContextId,
                new E_RepairChange(
                    "ISBN",
                    selectedOpportunity.Record?.Metadata?.Isbn,
                    approvedIsbn,
                    approvedCandidate.Source,
                    approvedCandidate.Evidence,
                    approvedCandidate.Confidence,
                    true));

            //---------------------------------------------------------
            // Report the approved repair.
            //
            // The actual EPUB modification will occur later when
            // the approved repair plan is executed.
            //---------------------------------------------------------

            CV_ActionResult result = new()
            {
                ActionId = request.ActionId,
                Success = true,
                Message =
                    $"I've recorded your approval of ISBN {approvedIsbn} " +
                    "for this ebook. The repair is ready for execution."
            };

            result.Evidence.Add(
                approvedCandidate.Evidence);

            result.Options.Add(
                new CV_ActionOption
    {
                    Id = "ApplyRepair",
                    ActionId = "ExecuteRepairPlan",
                    ContextId = request.ContextId,
                    Label = "Apply this repair",
                    Confidence = 1.0,
                    Source = "Ebook Expert"
    });

            return result;


        }

        /// <summary>
        /// Gets the ISBN explicitly approved for a specific original ebook.
        ///
        /// Returns null when the user has not selected an ISBN.
        /// </summary>
        public string? GetApprovedIsbn(
     string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                return null;

            E_RepairPlan? repairPlan =
    _repairService.GetRepairPlan(originalPath);

            if (repairPlan == null)
                return null;

            foreach (E_RepairChange change in repairPlan.Changes)
            {
                if (string.Equals(
                        change.RepairType,
                        "ISBN",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return change.ApprovedValue as string;
                }
            }

            return null;
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

                string originalPath =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                // ---------------------------------------------------------
                // If the action is associated with a specific ebook,
                // research only that ebook.
                //
                // This prevents ISBN candidates from multiple ebooks
                // being mixed into one set of choices.
                // ---------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(request.ContextId) &&
                    !string.Equals(
                        originalPath,
                        request.ContextId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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
                        ActionId = request.ActionId,

                        // Identifies the specific ebook this candidate belongs to.
                        //
                        // OriginalFullPath is used rather than CurrentFullPath
                        // because the current name/path may change during the
                        // workflow.
                        ContextId = originalPath,

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
                        string.IsNullOrWhiteSpace(request.ContextId)
                            ? "No ebooks with missing ISBN information were available for research."
                            : "The selected ebook could not be found among the current missing-ISBN opportunities."
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