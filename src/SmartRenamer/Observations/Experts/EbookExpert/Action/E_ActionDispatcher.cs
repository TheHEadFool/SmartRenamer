using Scout.Observations.Conversation;
using Scout.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

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
    /// • Return selectable options only when a user decision is genuinely required.
    /// • Keep autonomous repair activity out of the user conversation.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Interpret natural-language conversation.
    /// • Modify EPUB files directly.
    /// • Perform ISBN research directly.
    /// • Select an ISBN candidate automatically.
    ///
    /// Autonomous repairs that meet the Ebook repair policy are completed
    /// without exposing the internal candidate-selection process to the user.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_ActionDispatcher
    {
        private readonly E_RepairService _repairService = new();
        private readonly E_RepairDecisionEngine _repairDecisionEngine = new();

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
            IReadOnlyList<RepairOpportunity> opportunities,
            bool automaticAuthorization,
            double confidenceThreshold)
        {
            ArgumentNullException.ThrowIfNull(request);

            ArgumentNullException.ThrowIfNull(opportunities);

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
                        opportunities,
                        automaticAuthorization,
                        confidenceThreshold),

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
                RequiresReobservation = true,
                Message =
                    "The approved repair has been applied. " +
                    "The repaired EPUB is ready for the next investigation pass."
            };

            result.Evidence.Add(
                $"Completed repaired EPUB: {repairedPath}");

            return result;
        }

        /// <summary>
        /// Creates a user-facing ISBN action option while keeping the
        /// Conversation Framework domain-neutral.
        /// </summary>
        private static CV_ActionOption CreateIsbnActionOption(
            CV_ActionRequest request,
            string originalPath,
            string fileName,
            RepairDecisionCandidate candidate,
            string actionLabel)
        {
            CV_ActionOption option = new()
            {
                Id = candidate.Value?.ToString() ?? string.Empty,
                ActionId = request.ActionId,
                ContextId = originalPath,
                Label = BuildCandidateLabel(
                    candidate,
                    fileName,
                    actionLabel),
                Confidence = candidate.Confidence,
                Source = candidate.Source
            };

            foreach (string detail in candidate.Details)
            {
                if (!string.IsNullOrWhiteSpace(detail))
                    option.Evidence.Add(detail);
            }

            return option;
        }

        /// <summary>
        /// Builds a concise factual description suitable for an action button.
        /// </summary>
        private static string BuildCandidateLabel(
            RepairDecisionCandidate candidate,
            string fileName,
            string actionLabel)
        {
            List<string> lines = new()
            {
                $"{actionLabel}: {candidate.Value} — {fileName}"
            };

            return $"{actionLabel}: {candidate.Value} — {fileName}";
        }

        /// <summary>
        /// Builds the factual explanation Scout gives when it recommends one
        /// candidate over the alternatives.
        /// </summary>
        private static string BuildCandidateSummary(
            RepairDecisionCandidate candidate,
            string heading)
        {
            List<string> lines = new()
            {
                heading + ":"
            };

            lines.AddRange(candidate.Details);

            if (!string.IsNullOrWhiteSpace(candidate.Evidence))
                lines.Add("Evidence: " + candidate.Evidence);

            return string.Join(Environment.NewLine, lines);
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
                "for this ebook. Would you like me to apply this repair?"
            };

            result.Evidence.Add(
                approvedCandidate.Evidence);

            result.Options.Add(
                new CV_ActionOption
                {
                    Id = "ApplyRepair",
                    ActionId = "ExecuteRepairPlan",
                    ContextId = request.ContextId,
                    Label = "Click to apply this repair",
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
    IReadOnlyList<RepairOpportunity> opportunities,
    bool automaticAuthorization,
    double confidenceThreshold)
        {
            List<string> evidence = [];
            List<CV_ActionOption> options = [];

            int researchedBooks = 0;
            int candidateCount = 0;
            bool repairExecuted = false;
            bool retrySuggested = false;

            foreach (RepairOpportunity opportunity in opportunities)
            {
                if (!opportunity.MissingIsbn)
                    continue;

                string originalPath =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                //---------------------------------------------------------
                // If the action is associated with a specific ebook,
                // research only that ebook.
                //
                // This prevents ISBN candidates from multiple ebooks
                // being mixed into one set of choices.
                //---------------------------------------------------------

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
                    continue;
                }

                //---------------------------------------------------------
                // Evaluate the researched candidates against additional
                // evidence found inside the EPUB.
                //
                // The evidence evaluator does not approve or apply a
                // repair. It produces the generic candidates that the
                // Ebook repair decision engine evaluates.
                //---------------------------------------------------------

                List<RepairDecisionCandidate> evaluatedCandidates =
                    _repairService.EvaluateIsbnCandidates(
                        opportunity,
                        candidates);

                RepairDecisionResult decision =
                    _repairDecisionEngine.Evaluate(
                        evaluatedCandidates,
                        confidenceThreshold,
                        automaticAuthorization);

                if (decision.State ==
                    RepairRecommendation.RepairDecisionState.SafeToApply &&
                    decision.SelectedCandidate != null)
                {
                    //---------------------------------------------------------
                    // Scout has been authorized to handle qualifying repairs.
                    //
                    // The decision engine identified exactly one preferred
                    // candidate that meets the current confidence threshold.
                    //
                    // Add the approved change to the repair plan, then execute
                    // that plan. The repair service creates the repaired EPUB
                    // and updates the FileContext.CurrentFullPath.
                    //---------------------------------------------------------

                    RepairDecisionCandidate selectedCandidate =
                        decision.SelectedCandidate;

                    candidateCount++;

                    string approvedIsbn =
                        selectedCandidate.Value?.ToString() ?? string.Empty;

                    _repairService.AddRepairChange(
                        originalPath,
                        new E_RepairChange(
                            "ISBN",
                            opportunity.Record?.Metadata?.Isbn,
                            approvedIsbn,
                            selectedCandidate.Source,
                            selectedCandidate.Evidence,
                            selectedCandidate.Confidence,
                            true));

                    _repairService.ExecuteRepairPlan(opportunity);

                    repairExecuted = true;


                }
                else if (decision.State ==
                         RepairRecommendation.RepairDecisionState.UserDecisionRequired)
                {
                    //---------------------------------------------------------
                    // When Scout has identified one clearly preferred
                    // candidate, present that candidate as Scout's recommendation.
                    // The user still approves it because automatic repair has
                    // not been authorized.
                    //
                    // Only expose every candidate when Scout genuinely cannot
                    // distinguish between them.
                    //---------------------------------------------------------

                    List<RepairDecisionCandidate> preferredCandidates =
                        decision.Candidates
                            .Where(candidate => candidate.IsPreferred)
                            .ToList();

                    if (preferredCandidates.Count == 1)
                    {
                        RepairDecisionCandidate selectedCandidate =
                            preferredCandidates[0];

                        candidateCount++;

                        CV_ActionOption option =
                            CreateIsbnActionOption(
                                request,
                                originalPath,
                                fileName,
                                selectedCandidate,
                                "Use this ISBN");

                        option.Evidence.Add(
                            selectedCandidate.Evidence);

                        options.Add(option);

                        evidence.Add(
                            $"{fileName}: I found several possible ISBNs, but one is " +
                            "better supported by the evidence.");

                        evidence.Add(
                            BuildCandidateSummary(
                                selectedCandidate,
                                "My recommended ISBN"));

                        evidence.Add(
                            $"{fileName}: I have not changed the EPUB. " +
                            "Choose 'Use this ISBN' to approve this repair.");
                    }
                    else
                    {
                        foreach (RepairDecisionCandidate candidate in decision.Candidates)
                        {
                            candidateCount++;

                            CV_ActionOption option =
                                CreateIsbnActionOption(
                                    request,
                                    originalPath,
                                    fileName,
                                    candidate,
                                    "Use this ISBN");

                            option.Evidence.Add(
                                candidate.Evidence);

                            options.Add(option);
                        }

                        evidence.Add(
                            $"{fileName}: I found several possible ISBNs, but I " +
                            "could not safely distinguish between them. I have " +
                            "listed the facts for each candidate so you can choose.");
                    }
                }
                else if (decision.State ==
                         RepairRecommendation.RepairDecisionState.InsufficientEvidence)
                {
                    //---------------------------------------------------------
                    // Candidates were found, but none met the current
                    // confidence threshold.
                    //
                    // This is different from finding no candidates at all.
                    // The Ebook repair policy may therefore decide to retry
                    // this opportunity at a lower confidence threshold.
                    //---------------------------------------------------------

                    if (evaluatedCandidates.Count > 0)
                    {
                        retrySuggested = true;
                    }


                }


            }

            //---------------------------------------------------------
            // This check must occur after the search loop.
            //---------------------------------------------------------

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
                RequiresReobservation = repairExecuted,
                RetrySuggested = retrySuggested && !repairExecuted,
                Message = options.Count > 0
                    ? "I found an ISBN choice that needs your decision."
                    : string.Empty
            };

            // Evidence is user-facing only when Scout genuinely needs the
            // user's decision. Autonomous repair activity remains silent.
            if (options.Count > 0)
            {
                result.Evidence.AddRange(evidence);
            }

            result.Options.AddRange(options);

            //---------------------------------------------------------
            // A successful automatic repair changes the EPUB's current
            // file state. ProjectWorkflow will respond by re-observing it.
            //---------------------------------------------------------


            return result;
        }
    }
}