using Scout.Observations.Conversation;
using Scout.Observations.Experts.EbookExpert.Data;
using Scout.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
    ///
    /// Automatic repair uses the existing domain resources and decision engine
    /// and only acts when those components establish a safe candidate.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_ActionDispatcher
    {
        private readonly E_RepairService _repairService = new();
        private readonly E_RepairDecisionEngine _repairDecisionEngine = new();

        private readonly object _repairExecutionLock = new();

        //---------------------------------------------------------
        // Background external research
        //---------------------------------------------------------

        private readonly E_ExternalResearchCoordinator _externalResearchCoordinator =
            new();

        private readonly HashSet<string> _externalResearchingPaths =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<IsbnResearchCandidate>>
            _researchedIsbnCandidates =
                new(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _metadataResearchAttempts =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<MetadataResearchCandidate>>
            _researchedMetadataCandidates =
                new(StringComparer.OrdinalIgnoreCase);

        private readonly object _researchCacheLock = new();

        /// <summary>
        /// Raised when a background external-research action finishes.
        /// The Ebook Expert forwards the result through the generic
        /// Observation Framework so the workflow can re-observe the affected
        /// branch without teaching Scout infrastructure about ISBNs.
        /// </summary>
        public event Action<string, CV_ActionResult>? BackgroundActionCompleted;

        /// <summary>
        /// Returns true while external research is queued or running for the
        /// supplied EPUB.
        /// </summary>
        public bool IsExternalResearching(string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                return false;

            lock (_externalResearchingPaths)
            {
                return _externalResearchingPaths.Contains(originalPath) ||
                       _externalResearchingPaths.Contains("metadata:" + originalPath);
            }
        }

        public int ExternalResearchCount
        {
            get
            {
                lock (_externalResearchingPaths)
                {
                    return _externalResearchingPaths.Count;
                }
            }
        }

        //---------------------------------------------------------
        // Automatic recovery attempts
        //---------------------------------------------------------
        //
        // Automatic recovery should use an existing research capability,
        // but it must not repeatedly call the same external resource for
        // an unresolved ebook every time another ebook happens to be
        // repaired. One automatic attempt per ebook is enough until the
        // user supplies new evidence.
        //
        //---------------------------------------------------------

        private readonly HashSet<string> _automaticIsbnResearchAttempts =
            new(StringComparer.OrdinalIgnoreCase);

        public void ResetAutomaticRecoveryState()
        {
            _automaticIsbnResearchAttempts.Clear();
            _metadataResearchAttempts.Clear();
            _externalResearchCoordinator.Reset();

            lock (_externalResearchingPaths)
            {
                _externalResearchingPaths.Clear();
            }

            lock (_researchCacheLock)
            {
                _researchedIsbnCandidates.Clear();
            }
        }

        public void ResetAutomaticRecoveryAttempt(string originalPath)
        {
            if (!string.IsNullOrWhiteSpace(originalPath))
            {
                _automaticIsbnResearchAttempts.Remove(originalPath);
                _metadataResearchAttempts.Remove(originalPath);

                lock (_researchCacheLock)
                {
                    _researchedIsbnCandidates.Remove(originalPath);
                    _researchedMetadataCandidates.Remove(originalPath);
                }
            }
        }

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
            string? userEvidence = null,
            Func<bool>? automaticAuthorizationProvider = null,
            Func<string, RepairOpportunity?>? currentOpportunityResolver = null)
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
                (string.Equals(
                    request.ActionId,
                    "ResearchMissingIsbn",
                    StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(
                    request.ActionId,
                    "BackgroundResearchMissingIsbn",
                    StringComparison.OrdinalIgnoreCase)))
            {
                return SelectIsbnCandidate(
                    request,
                    opportunities);
            }

            if (!string.IsNullOrWhiteSpace(request.OptionId) &&
                string.Equals(
                    request.ActionId,
                    "ReconcileMetadataIdentity",
                    StringComparison.OrdinalIgnoreCase))
            {
                return ApproveIdentityCandidate(
                    request,
                    opportunities);
            }

            if (!string.IsNullOrWhiteSpace(request.OptionId) &&
                string.Equals(
                    request.ActionId,
                    "SelectMetadataCandidate",
                    StringComparison.OrdinalIgnoreCase))
            {
                return SelectMetadataCandidate(
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
                        userEvidence,
                        automaticAuthorizationProvider,
                        currentOpportunityResolver),

                "ResearchMissingMetadata" =>
                    ResearchMissingMetadata(
                        request,
                        opportunities,
                        automaticAuthorization,
                        automaticAuthorizationProvider,
                        currentOpportunityResolver),

                "ExecuteRepairPlan" =>
                    ExecuteRepairPlan(
                        request,
                        opportunities),

                "ReconcileMetadataIdentity" =>
                    ReviewMetadataIdentity(
                        request,
                        opportunities),

                "ReviewUnsupportedRepair" =>
                    ReviewUnsupportedRepair(
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
            lock (_repairExecutionLock)
            {
                return ExecuteRepairPlanCore(
                    request,
                    opportunities);
            }
        }

        private CV_ActionResult ExecuteRepairPlanCore(
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
        /// Adds one explicitly user-provided metadata value to the normal
        /// repair plan. The user has supplied the value in direct response to
        /// Scout's request for that specific field, so this is an approved
        /// repair candidate rather than an inferred value.
        /// </summary>
        internal CV_ActionResult ApplyUserProvidedMetadata(
            CV_ActionRequest request,
            RepairOpportunity opportunity,
            string field,
            string value,
            bool automaticAuthorization)
        {
            lock (_repairExecutionLock)
            {
                if (string.IsNullOrWhiteSpace(request.ContextId) ||
                    opportunity?.Record?.Metadata == null ||
                    string.IsNullOrWhiteSpace(field) ||
                    string.IsNullOrWhiteSpace(value))
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message =
                            "I couldn't match the information you supplied to the ebook or metadata field that needs repair."
                    };
                }

                string repairType = field.Trim();
                string approvedValue = value.Trim();
                string currentValue = GetCurrentMetadataValue(
                    opportunity.Record.Metadata,
                    repairType);

                if (!IsSupportedUserMetadataField(repairType))
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message =
                            $"Scout cannot currently apply user-provided '{repairType}' metadata through the text conversation."
                    };
                }

                if (string.Equals(
                        repairType,
                        "ISBN",
                        StringComparison.OrdinalIgnoreCase))
                {
                    IReadOnlyList<string> recognized =
                        new E_IsbnContentRecognizer().Recognize(approvedValue);

                    if (recognized.Count != 1)
                    {
                        return new CV_ActionResult
                        {
                            ActionId = request.ActionId,
                            Success = false,
                            Message =
                                "I couldn't validate the ISBN you supplied. Please provide a valid ISBN-10 or ISBN-13."
                        };
                    }

                    approvedValue = recognized[0];
                }

                if (string.Equals(
                        currentValue.Trim(),
                        approvedValue,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = true,
                        Message =
                            $"Scout already has that {repairType} value; no repair is needed for this field."
                    };
                }

                _repairService.AddRepairChange(
                    request.ContextId,
                    new E_RepairChange(
                        repairType,
                        string.IsNullOrWhiteSpace(currentValue)
                            ? null
                            : currentValue,
                        approvedValue,
                        "UserProvided",
                        $"The user explicitly supplied the {repairType} value during the repair conversation.",
                        1.0,
                        true));

                if (automaticAuthorization)
                {
                    string? repairedPath =
                        _repairService.ExecuteRepairPlan(opportunity);

                    if (!string.IsNullOrWhiteSpace(repairedPath))
                    {
                        return new CV_ActionResult
                        {
                            ActionId = request.ActionId,
                            Success = true,
                            RequiresReobservation = true,
                            Message =
                                $"I used the {repairType} you supplied, applied the repair to the protected working copy, and will re-check the ebook."
                        };
                    }

                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message =
                            $"I accepted the {repairType} you supplied, but I could not apply the repair safely. The original EPUB was not modified."
                    };
                }

                CV_ActionResult result = new()
                {
                    ActionId = request.ActionId,
                    Success = true,
                    Message =
                        $"I have recorded the {repairType} you supplied and added it to the repair plan. The original EPUB has not been modified."
                };

                result.Options.Add(
                    new CV_ActionOption
                    {
                        Id = "ApplyRepair",
                        ActionId = "ExecuteRepairPlan",
                        ContextId = request.ContextId,
                        Label = $"Apply this {repairType} repair",
                        Confidence = 1.0,
                        Source = "Ebook Expert"
                    });

                return result;
            }
        }

        private static bool IsSupportedUserMetadataField(string field)
        {
            return field.Equals("Title", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("Author", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("Series", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("SeriesNumber", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("ISBN", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("Publisher", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("Language", StringComparison.OrdinalIgnoreCase) ||
                   field.Equals("Description", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetCurrentMetadataValue(
            Scout.Observations.Experts.EbookExpert.Data.E_EbookMetadata metadata,
            string field)
        {
            return field.ToLowerInvariant() switch
            {
                "title" => metadata.Title ?? string.Empty,
                "author" => metadata.Author ?? string.Empty,
                "series" => metadata.Series ?? string.Empty,
                "seriesnumber" => metadata.SeriesNumber ?? string.Empty,
                "isbn" => metadata.Isbn ?? string.Empty,
                "publisher" => metadata.Publisher ?? string.Empty,
                "language" => metadata.Language ?? string.Empty,
                "description" => metadata.Description ?? string.Empty,
                _ => string.Empty
            };
        }

        /// <summary>
        /// Creates a user-facing ISBN action option while keeping the
        /// Conversation Framework domain-neutral.
        /// </summary>
        private static CV_ActionOption CreateUserDecisionOption(
            string originalPath,
            string id,
            string label,
            string evidence)
        {
            CV_ActionOption option = new()
            {
                Id = id,
                ActionId = id,
                ContextId = originalPath,
                Label = label,
                AcceptsUserInput = string.Equals(
                    id,
                    "AddRepairInformation",
                    StringComparison.OrdinalIgnoreCase)
            };

            option.Evidence.Add(evidence);
            return option;
        }

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
        /// Applies every repair that the current evidence already establishes
        /// as safe to perform without asking the user.
        ///
        /// This is the internal automatic-repair pass used by EbookExpert
        /// during normal investigation. It deliberately uses the existing
        /// ISBN research, ISBN evidence evaluation, repair decision engine,
        /// identity evaluator, and repair service rather than creating a
        /// second repair path.
        ///
        /// A repair is only performed when the existing domain machinery has
        /// already established a safe candidate. Ambiguous or unresolved
        /// opportunities are left untouched so they can become waiting
        /// branches for later user interaction.
        /// </summary>
        public bool ApplySafeAutomaticRepairs(
            IReadOnlyList<RepairOpportunity> opportunities,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider = null,
            Func<string, RepairOpportunity?>? currentOpportunityResolver = null)
        {
            lock (_repairExecutionLock)
            {
                return ApplySafeAutomaticRepairsCore(
                    opportunities,
                    automaticAuthorization,
                    automaticAuthorizationProvider,
                    currentOpportunityResolver);
            }
        }

        private bool ApplySafeAutomaticRepairsCore(
            IReadOnlyList<RepairOpportunity> opportunities,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider = null,
            Func<string, RepairOpportunity?>? currentOpportunityResolver = null)
        {
            ArgumentNullException.ThrowIfNull(opportunities);

            bool repairApplied = false;

            foreach (RepairOpportunity opportunity in opportunities)
            {
                if (opportunity?.Record?.Metadata == null)
                    continue;

                string originalPath =
                    opportunity.Record.File?.OriginalFullPath
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(originalPath))
                    continue;

                //---------------------------------------------------------
                // FIRST: establish the strongest local identity Scout can
                // establish from the EPUB and its collected evidence.
                //
                // This must happen before any external research decision.
                // A mutable filename is supporting evidence; it is never
                // allowed to cause external research to consume an identity
                // that Scout has already determined needs reconciliation.
                //---------------------------------------------------------
                E_BookIdentityEvaluator identityEvaluator = new();

                BookIdentityEvaluation evaluation =
                    identityEvaluator.Evaluate(opportunity.Record);

                opportunity.IdentityEvaluation = evaluation;

                if (opportunity.MissingIsbn || evaluation.RepairRequired)
                {
                    Debug.WriteLine(
                        $"[IDENTITY TRACE] LOCAL | " +
                        $"{opportunity.Record.File?.CurrentName ?? "Unknown ebook"} | " +
                        $"ObservedTitle='{opportunity.Record.Metadata.Title}' | " +
                        $"ObservedAuthor='{opportunity.Record.Metadata.Author}' | " +
                        $"CandidateTitle='{evaluation.Candidate?.Title ?? ""}' | " +
                        $"CandidateAuthor='{evaluation.Candidate?.Authors ?? ""}' | " +
                        $"CandidateSeries='{evaluation.Candidate?.Series ?? ""}' | " +
                        $"CandidateSeriesNumber='{evaluation.Candidate?.SeriesNumber ?? ""}' | " +
                        $"RepairRequired={evaluation.RepairRequired} | " +
                        $"Reason='{evaluation.Reason}'");
                }

                //---------------------------------------------------------
                // If automatic physical repair is not authorized, do not
                // send ISBN research to an external provider while Title or
                // Author still needs local reconciliation. Research can wait
                // until the identity is established or the user supplies the
                // missing decision.
                //---------------------------------------------------------
                if (!automaticAuthorization)
                {
                    QueueBackgroundRecoveryIfNeeded(
                        opportunity,
                        automaticAuthorization,
                        automaticAuthorizationProvider,
                        currentOpportunityResolver,
                        evaluation);

                    continue;
                }

                //---------------------------------------------------------
                // SECOND: apply deterministic local identity repairs before
                // researching anything outside the EPUB. The next automatic
                // pass will re-observe the working copy, so external research
                // never has to treat a repair conclusion as new EPUB evidence.
                //---------------------------------------------------------
                bool identityRepairApplied =
                    ApplySafeIdentityRepair(
                        opportunity,
                        evaluation,
                        originalPath);

                if (identityRepairApplied)
                {
                    repairApplied = true;

                    // Do NOT queue external research against the stale
                    // MetadataRecord. EbookExpert will re-observe the working
                    // EPUB and run the complete investigation again.
                    continue;
                }

                //---------------------------------------------------------
                // THIRD: use ISBN evidence already found inside the EPUB.
                // A single valid local ISBN is stronger than external data.
                //---------------------------------------------------------
                if (opportunity.MissingIsbn)
                {
                    bool localIsbnRepairApplied = false;

                    List<IsbnResearchCandidate> localIsbnCandidates =
                        _repairService.RecognizeIsbnCandidates(opportunity);

                    if (localIsbnCandidates.Count == 1)
                    {
                        List<RepairDecisionCandidate> evaluatedLocalCandidates =
                            _repairService.EvaluateIsbnCandidates(
                                opportunity,
                                localIsbnCandidates);

                        RepairDecisionResult localDecision =
                            _repairDecisionEngine.Evaluate(
                                evaluatedLocalCandidates,
                                E_RepairDecisionEngine.MinimumConfidenceThreshold,
                                automaticAuthorization);

                        if (localDecision.State ==
                                RepairRecommendation.RepairDecisionState.SafeToApply &&
                            localDecision.SelectedCandidate != null)
                        {
                            RepairDecisionCandidate selectedLocalCandidate =
                                localDecision.SelectedCandidate;

                            string approvedLocalIsbn =
                                selectedLocalCandidate.Value?.ToString() ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(approvedLocalIsbn))
                            {
                                _repairService.AddRepairChange(
                                    originalPath,
                                    new E_RepairChange(
                                        "ISBN",
                                        opportunity.Record.Metadata.Isbn,
                                        approvedLocalIsbn,
                                        selectedLocalCandidate.Source,
                                        selectedLocalCandidate.Evidence,
                                        selectedLocalCandidate.Confidence,
                                        true));

                                if (!string.IsNullOrWhiteSpace(
                                        _repairService.ExecuteRepairPlan(opportunity)))
                                {
                                    repairApplied = true;
                                    localIsbnRepairApplied = true;
                                }
                            }
                        }
                    }

                    //---------------------------------------------------------
                    // FOURTH: external research is allowed only after the
                    // local identity is ready. The research resource receives
                    // the reconciled identity as query input, without treating
                    // that conclusion as newly observed EPUB metadata.
                    //---------------------------------------------------------
                    bool identityReadyForExternalResearch =
                        IsIdentityReadyForExternalResearch(
                            opportunity,
                            evaluation);

                    Debug.WriteLine(
                        $"[IDENTITY TRACE] ISBN GATE | " +
                        $"{opportunity.Record.File?.CurrentName ?? "Unknown ebook"} | " +
                        $"Ready={identityReadyForExternalResearch} | " +
                        $"Title='{evaluation.Candidate?.Title ?? opportunity.Record.Metadata.Title}' | " +
                        $"Author='{evaluation.Candidate?.Authors ?? opportunity.Record.Metadata.Author}'");

                    if (!localIsbnRepairApplied &&
                        identityReadyForExternalResearch &&
                        _automaticIsbnResearchAttempts.Add(originalPath))
                    {
                        QueueAutomaticIsbnResearch(
                            opportunity,
                            automaticAuthorization,
                            automaticAuthorizationProvider,
                            currentOpportunityResolver);
                    }
                }

                //---------------------------------------------------------
                // Other external metadata research follows the same identity
                // gate. It must not query a provider with a known-bad
                // Title/Author identity.
                //---------------------------------------------------------
                QueueBackgroundRecoveryIfNeeded(
                    opportunity,
                    automaticAuthorization,
                    automaticAuthorizationProvider,
                    currentOpportunityResolver,
                    evaluation);
            }

            return repairApplied;
        }

        private bool ApplySafeIdentityRepair(
            RepairOpportunity opportunity,
            BookIdentityEvaluation evaluation,
            string originalPath)
        {
            if (!evaluation.RepairRequired ||
                evaluation.Candidate == null)
            {
                return false;
            }

            BookIdentityCandidate candidate = evaluation.Candidate;

            string evidence = string.Join(
                Environment.NewLine,
                evaluation.Evidence.Where(
                    item => !string.IsNullOrWhiteSpace(item)));

            if (string.IsNullOrWhiteSpace(evidence))
                evidence = evaluation.Reason;

            const string source = "LocalIdentityEvidence";
            const double establishedConfidence = 1.0;

            int changesAdded = 0;

            if (!string.IsNullOrWhiteSpace(candidate.Title) &&
                !string.Equals(
                    opportunity.Record.Metadata.Title,
                    candidate.Title,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    originalPath,
                    new E_RepairChange(
                        "Title",
                        opportunity.Record.Metadata.Title,
                        candidate.Title,
                        source,
                        evidence,
                        establishedConfidence,
                        true));

                changesAdded++;
            }

            if (!string.IsNullOrWhiteSpace(candidate.Authors) &&
                !string.Equals(
                    opportunity.Record.Metadata.Author,
                    candidate.Authors,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    originalPath,
                    new E_RepairChange(
                        "Author",
                        opportunity.Record.Metadata.Author,
                        candidate.Authors,
                        source,
                        evidence,
                        establishedConfidence,
                        true));

                changesAdded++;
            }

            bool seriesResolved =
                evaluation.SeriesEvaluation?.State ==
                SeriesEvidenceState.Resolved;

            if (seriesResolved &&
                !string.IsNullOrWhiteSpace(candidate.Series) &&
                !string.Equals(
                    opportunity.Record.Metadata.Series,
                    candidate.Series,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    originalPath,
                    new E_RepairChange(
                        "Series",
                        opportunity.Record.Metadata.Series,
                        candidate.Series,
                        source,
                        evidence,
                        establishedConfidence,
                        true));

                changesAdded++;
            }

            if (seriesResolved &&
                !string.IsNullOrWhiteSpace(candidate.SeriesNumber) &&
                !string.Equals(
                    opportunity.Record.Metadata.SeriesNumber,
                    candidate.SeriesNumber,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    originalPath,
                    new E_RepairChange(
                        "SeriesNumber",
                        opportunity.Record.Metadata.SeriesNumber,
                        candidate.SeriesNumber,
                        source,
                        evidence,
                        establishedConfidence,
                        true));

                changesAdded++;
            }

            if (changesAdded == 0)
                return false;

            return !string.IsNullOrWhiteSpace(
                _repairService.ExecuteRepairPlan(opportunity));
        }

        private static bool IsIdentityReadyForExternalResearch(
            RepairOpportunity opportunity,
            BookIdentityEvaluation evaluation)
        {
            if (opportunity.Record?.Metadata == null)
                return false;

            string title = opportunity.Record.Metadata.Title?.Trim() ?? string.Empty;
            string author = opportunity.Record.Metadata.Author?.Trim() ?? string.Empty;

            //-------------------------------------------------------------
            // If the local evaluator has a Title/Author correction pending,
            // the observed metadata is not yet a safe external-research
            // identity. Automatic repair must happen first and the EPUB must
            // be re-observed. A resolved Series is helpful but not required
            // to research an ISBN.
            //-------------------------------------------------------------
            if (evaluation.RepairRequired && evaluation.Candidate != null)
            {
                string candidateTitle = evaluation.Candidate.Title?.Trim() ?? string.Empty;
                string candidateAuthor = evaluation.Candidate.Authors?.Trim() ?? string.Empty;

                bool titleNeedsRepair =
                    !string.IsNullOrWhiteSpace(candidateTitle) &&
                    !string.Equals(title, candidateTitle, StringComparison.OrdinalIgnoreCase);

                bool authorNeedsRepair =
                    !string.IsNullOrWhiteSpace(candidateAuthor) &&
                    !string.Equals(author, candidateAuthor, StringComparison.OrdinalIgnoreCase);

                if (titleNeedsRepair || authorNeedsRepair)
                    return false;
            }

            return !string.IsNullOrWhiteSpace(title) &&
                   !string.IsNullOrWhiteSpace(author);
        }

        private void QueueBackgroundRecoveryIfNeeded(
            RepairOpportunity opportunity,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider,
            Func<string, RepairOpportunity?>? currentOpportunityResolver,
            BookIdentityEvaluation evaluation)
        {
            if (opportunity?.Record?.Metadata == null)
                return;

            if ((opportunity.MissingPublisher || opportunity.MissingDescription) &&
                IsIdentityReadyForExternalResearch(
                    opportunity,
                    evaluation) &&
                !string.IsNullOrWhiteSpace(opportunity.Record.File?.OriginalFullPath))
            {
                string path = opportunity.Record.File!.OriginalFullPath;

                if (_metadataResearchAttempts.Add(path))
                {
                    QueueMetadataResearch(
                        opportunity,
                        automaticAuthorization,
                        automaticAuthorizationProvider,
                        currentOpportunityResolver);
                }
            }
        }

        private void QueueMetadataResearch(
            RepairOpportunity opportunity,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider,
            Func<string, RepairOpportunity?>? currentOpportunityResolver)
        {
            string originalPath =
                opportunity.Record?.File?.OriginalFullPath ?? string.Empty;

            if (string.IsNullOrWhiteSpace(originalPath))
                return;

            lock (_externalResearchingPaths)
            {
                _externalResearchingPaths.Add("metadata:" + originalPath);
            }

            bool queued =
                _externalResearchCoordinator.Enqueue(
                    "metadata:" + originalPath,
                    cancellationToken =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        RepairOpportunity currentOpportunity =
                            currentOpportunityResolver?.Invoke(originalPath)
                            ?? opportunity;

                        MetadataResearchResult research =
                            new E_MetadataResearchResource().Research(
                                currentOpportunity.Record.Metadata,
                                currentOpportunity.Record.Evidence);

                        cancellationToken.ThrowIfCancellationRequested();

                        return Task.FromResult(
                            CompleteMetadataResearch(
                                currentOpportunity,
                                research,
                                automaticAuthorization,
                                automaticAuthorizationProvider));
                    },
                    result =>
                    {
                        lock (_externalResearchingPaths)
                        {
                            _externalResearchingPaths.Remove("metadata:" + originalPath);
                        }

                        BackgroundActionCompleted?.Invoke(
                            originalPath,
                            result);
                    });

            if (!queued)
            {
                lock (_externalResearchingPaths)
                {
                    _externalResearchingPaths.Remove("metadata:" + originalPath);
                }
            }
        }

        private CV_ActionResult CompleteMetadataResearch(
            RepairOpportunity opportunity,
            MetadataResearchResult research,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider)
        {
            lock (_repairExecutionLock)
            {
                string originalPath =
                    opportunity.Record.File?.OriginalFullPath ?? string.Empty;
                string fileName =
                    opportunity.Record.File?.CurrentName ?? "Unknown ebook";
                bool authorized =
                    automaticAuthorizationProvider?.Invoke() ?? automaticAuthorization;

                E_EbookMetadata? currentMetadata = null;

                if (opportunity.Record.File != null)
                {
                    currentMetadata =
                        E_EbookMetadataReader.Read(
                            opportunity.Record.File);
                }

                bool workingCopyAlreadyResolvedTarget =
                    currentMetadata != null &&
                    ((!opportunity.MissingPublisher &&
                      !opportunity.MissingDescription) ||
                     (opportunity.MissingPublisher &&
                      !string.IsNullOrWhiteSpace(currentMetadata.Publisher)) ||
                     (opportunity.MissingDescription &&
                      !string.IsNullOrWhiteSpace(currentMetadata.Description)));

                if (currentMetadata != null)
                {
                    opportunity.Record.Metadata.Title = currentMetadata.Title;
                    opportunity.Record.Metadata.Author = currentMetadata.Author;
                    opportunity.Record.Metadata.Publisher = currentMetadata.Publisher;
                    opportunity.Record.Metadata.Language = currentMetadata.Language;
                    opportunity.Record.Metadata.Isbn = currentMetadata.Isbn;
                    opportunity.Record.Metadata.Series = currentMetadata.Series;
                    opportunity.Record.Metadata.SeriesNumber = currentMetadata.SeriesNumber;
                    opportunity.Record.Metadata.Description = currentMetadata.Description;
                    opportunity.Record.Metadata.HasCover = currentMetadata.HasCover;
                    opportunity.Record.Metadata.CoverImage = currentMetadata.CoverImage;
                }

                if (research.Candidates.Count > 0)
                {
                    lock (_researchCacheLock)
                    {
                        _researchedMetadataCandidates[originalPath] =
                            research.Candidates.ToList();
                    }
                }

                List<string> resultEvidence = new();
                List<CV_ActionOption> resultOptions = new();
                int repairChangesAdded = 0;
                bool repairExecuted = false;

                string resultMessage =
                    research.ProviderUnavailable
                        ? $"Publisher/Summary research for {fileName} could not reach the external sources. Scout will keep the book unresolved rather than treating the connection failure as missing information."
                        : research.TimedOut
                            ? $"Publisher/Summary research for {fileName} timed out. Scout will keep the book unresolved rather than treating the timeout as missing information."
                            : research.Candidates.Count == 0
                                ? workingCopyAlreadyResolvedTarget
                                    ? $"Scout re-checked {fileName} and found that its protected working copy already contains the metadata this research was looking for."
                                    : $"Scout researched {fileName}, but could not establish a safe Publisher or Summary candidate."
                                : $"Scout found additional Publisher/Summary evidence for {fileName}.";

                foreach (MetadataResearchCandidate candidate in
                         research.Candidates
                             .GroupBy(
                                 item => item.Field,
                                 StringComparer.OrdinalIgnoreCase)
                             .Select(group =>
                                 group.OrderByDescending(
                                     item => item.Confidence)
                                     .First()))
                {
                    if (currentMetadata != null &&
                        !string.IsNullOrWhiteSpace(
                            GetCurrentMetadataValue(
                                currentMetadata,
                                candidate.Field)))
                    {
                        resultEvidence.Add(
                            $"{fileName}: Scout ignored the researched {candidate.Field} because the protected working copy already contains a value for that field.");
                        continue;
                    }

                    resultEvidence.Add(candidate.Evidence);

                    RepairDecisionCandidate metadataDecisionCandidate = new()
                    {
                        Value = candidate.Value,
                        Source = candidate.Source,
                        Evidence = candidate.Evidence,
                        Confidence = candidate.Confidence,
                        IsPreferred = true
                    };

                    foreach (string detail in candidate.Details)
                        metadataDecisionCandidate.Details.Add(detail);

                    RepairDecisionResult decision =
                        _repairDecisionEngine.Evaluate(
                            new[] { metadataDecisionCandidate },
                            E_RepairDecisionEngine.MinimumConfidenceThreshold,
                            authorized);

                    if (decision.State ==
                            RepairRecommendation.RepairDecisionState.SafeToApply &&
                        decision.SelectedCandidate != null)
                    {
                        _repairService.AddRepairChange(
                            originalPath,
                            new E_RepairChange(
                                candidate.Field,
                                GetCurrentMetadataValue(
                                    opportunity.Record.Metadata,
                                    candidate.Field),
                                candidate.Value,
                                candidate.Source,
                                candidate.Evidence,
                                candidate.Confidence,
                                true));

                        repairChangesAdded++;
                    }
                    else if (decision.State ==
                             RepairRecommendation.RepairDecisionState.UserDecisionRequired)
                    {
                        resultOptions.Add(
                            new CV_ActionOption
                            {
                                Id = candidate.Field + "|" + candidate.Value,
                                ActionId = "SelectMetadataCandidate",
                                ContextId = originalPath,
                                Label = $"Use researched {candidate.Field}: {candidate.Value}",
                                Confidence = candidate.Confidence,
                                Source = candidate.Source,
                                Evidence = { candidate.Evidence }
                            });
                    }
                }

                //-------------------------------------------------------------
                // Execute only if at least one candidate actually produced a
                // safe repair change. Candidate discovery alone never means
                // that a repair plan should execute.
                //-------------------------------------------------------------
                if (authorized && repairChangesAdded > 0)
                {
                    string? repairedPath =
                        _repairService.ExecuteRepairPlan(opportunity);

                    repairExecuted = !string.IsNullOrWhiteSpace(repairedPath);
                }

                foreach (string detail in research.Candidates.Count == 0
                    ? Array.Empty<string>()
                    : research.Candidates
                        .SelectMany(candidate => candidate.Details)
                        .Where(detail => !string.IsNullOrWhiteSpace(detail))
                        .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    resultEvidence.Add(detail);
                }

                CV_ActionResult result = new()
                {
                    ActionId = "BackgroundResearchMissingMetadata",
                    Success = true,
                    RequiresReobservation =
                        repairExecuted || workingCopyAlreadyResolvedTarget,
                    Message = resultMessage
                };

                result.Evidence.AddRange(resultEvidence);
                result.Options.AddRange(resultOptions);
                return result;
            }
        }

        private CV_ActionResult ResearchMissingMetadata(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider,
            Func<string, RepairOpportunity?>? currentOpportunityResolver)
        {
            if (!string.IsNullOrWhiteSpace(request.ContextId))
            {
                RepairOpportunity? opportunity =
                    FindOpportunity(request.ContextId, opportunities);

                if (opportunity != null)
                {
                    _metadataResearchAttempts.Remove(request.ContextId);
                    QueueMetadataResearch(
                        opportunity,
                        automaticAuthorization,
                        automaticAuthorizationProvider,
                        currentOpportunityResolver);
                }
            }
            else
            {
                foreach (RepairOpportunity opportunity in opportunities)
                {
                    if (opportunity.MissingPublisher || opportunity.MissingDescription)
                    {
                        string path = opportunity.Record?.File?.OriginalFullPath ?? string.Empty;
                        _metadataResearchAttempts.Remove(path);
                        QueueMetadataResearch(
                            opportunity,
                            automaticAuthorization,
                            automaticAuthorizationProvider,
                            currentOpportunityResolver);
                    }
                }
            }

            return new CV_ActionResult
            {
                ActionId = request.ActionId,
                Success = true,
                Message = "I am researching missing Publisher and Summary information in the background while Scout continues processing the collection."
            };
        }

        private CV_ActionResult SelectMetadataCandidate(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            if (string.IsNullOrWhiteSpace(request.ContextId) ||
                string.IsNullOrWhiteSpace(request.OptionId))
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message = "I could not determine which metadata candidate you selected."
                };
            }

            string[] parts = request.OptionId.Split('|', 2);
            if (parts.Length != 2)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message = "I could not interpret the researched metadata selection."
                };
            }

            string field = parts[0];
            string value = parts[1];

            RepairOpportunity? opportunity =
                FindOpportunity(request.ContextId, opportunities);

            if (opportunity == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message = "I could not match that metadata selection to the ebook."
                };
            }

            lock (_repairExecutionLock)
            {
                List<MetadataResearchCandidate> candidates;
                lock (_researchCacheLock)
                {
                    candidates = _researchedMetadataCandidates.TryGetValue(
                        request.ContextId,
                        out List<MetadataResearchCandidate>? cached)
                        ? cached.ToList()
                        : new List<MetadataResearchCandidate>();
                }

                MetadataResearchCandidate? selected = candidates.FirstOrDefault(
                    candidate =>
                        string.Equals(candidate.Field, field, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(candidate.Value, value, StringComparison.Ordinal));

                if (selected == null)
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message = "I could not verify that metadata candidate against Scout's research results."
                    };
                }

                _repairService.AddRepairChange(
                    request.ContextId,
                    new E_RepairChange(
                        field,
                        GetCurrentMetadataValue(opportunity.Record.Metadata, field),
                        selected.Value,
                        selected.Source,
                        selected.Evidence,
                        selected.Confidence,
                        true));

                CV_ActionResult result = new()
                {
                    ActionId = request.ActionId,
                    Success = true,
                    Message = $"I recorded the researched {field} for this ebook. The original EPUB has not been changed yet."
                };

                result.Options.Add(
                    new CV_ActionOption
                    {
                        Id = "ApplyRepair",
                        ActionId = "ExecuteRepairPlan",
                        ContextId = request.ContextId,
                        Label = $"Apply this {field} repair",
                        Confidence = selected.Confidence,
                        Source = selected.Source
                    });

                return result;
            }
        }

        /// <summary>
        /// Queues one ISBN recovery job. External research is deliberately
        /// removed from the synchronous EbookExpert investigation path.
        /// Only one external research job runs at a time so the provider is
        /// not flooded, while local Scout processing can continue.
        /// </summary>
        private void QueueAutomaticIsbnResearch(
            RepairOpportunity opportunity,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider,
            Func<string, RepairOpportunity?>? currentOpportunityResolver)
        {
            string originalPath =
                opportunity.Record?.File?.OriginalFullPath
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(originalPath))
                return;

            lock (_externalResearchingPaths)
            {
                _externalResearchingPaths.Add(originalPath);
            }

            bool queued =
                _externalResearchCoordinator.Enqueue(
                    originalPath,
                    cancellationToken =>
                    {
                        // The current ISBN resource is synchronous by design.
                        // The coordinator owns the background worker, so this
                        // call no longer blocks EbookExpert.Investigate().
                        // Cancellation is checked before the external call;
                        // the resource itself remains protected by its timeout.
                        cancellationToken.ThrowIfCancellationRequested();

                        RepairOpportunity? currentOpportunity =
                            currentOpportunityResolver?.Invoke(
                                originalPath)
                            ?? opportunity;

                        if (currentOpportunity == null ||
                            !currentOpportunity.MissingIsbn)
                        {
                            return Task.FromResult(
                                new CV_ActionResult
                                {
                                    ActionId = "BackgroundResearchMissingIsbn",
                                    Success = true,
                                    RequiresReobservation = false,
                                    Message =
                                        "This ebook no longer needs the queued ISBN research, so Scout did not perform another external lookup."
                                });
                        }

                        E_BookIdentityEvaluator currentIdentityEvaluator =
                            new();

                        BookIdentityEvaluation currentIdentityEvaluation =
                            currentIdentityEvaluator.Evaluate(
                                currentOpportunity.Record);

                        currentOpportunity.IdentityEvaluation =
                            currentIdentityEvaluation;

                        if (!IsIdentityReadyForExternalResearch(
                                currentOpportunity,
                                currentIdentityEvaluation))
                        {
                            return Task.FromResult(
                                new CV_ActionResult
                                {
                                    ActionId = "BackgroundResearchMissingIsbn",
                                    Success = true,
                                    RequiresReobservation = false,
                                    Message =
                                        "Scout did not perform external ISBN research because the ebook's local Title/Author identity still needs reconciliation."
                                });
                        }

                        IsbnResearchResult research =
                            _repairService.ResearchMissingIsbnWithStatus(
                                currentOpportunity,
                                null,
                                currentIdentityEvaluation);

                        cancellationToken.ThrowIfCancellationRequested();

                        return Task.FromResult(
                            CompleteAutomaticIsbnResearch(
                                currentOpportunity,
                                research,
                                automaticAuthorization,
                                automaticAuthorizationProvider));
                    },
                    result =>
                    {
                        lock (_externalResearchingPaths)
                        {
                            _externalResearchingPaths.Remove(originalPath);
                        }

                        BackgroundActionCompleted?.Invoke(
                            originalPath,
                            result);
                    });

            if (!queued)
            {
                lock (_externalResearchingPaths)
                {
                    _externalResearchingPaths.Remove(originalPath);
                }
            }
        }

        /// <summary>
        /// Evaluates a completed background ISBN search using the same repair
        /// decision engine and physical repair service used by the proven
        /// synchronous path.
        /// </summary>
        private CV_ActionResult CompleteAutomaticIsbnResearch(
            RepairOpportunity opportunity,
            IsbnResearchResult research,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider)
        {
            lock (_repairExecutionLock)
            {
                return CompleteAutomaticIsbnResearchCore(
                    opportunity,
                    research,
                    automaticAuthorization,
                    automaticAuthorizationProvider);
            }
        }

        private CV_ActionResult CompleteAutomaticIsbnResearchCore(
            RepairOpportunity opportunity,
            IsbnResearchResult research,
            bool automaticAuthorization,
            Func<bool>? automaticAuthorizationProvider)
        {
            //-------------------------------------------------------------
            // Re-read the protected working copy before evaluating external
            // ISBN evidence. A queued research result can be older than a
            // local repair or another background completion.
            //-------------------------------------------------------------
            if (opportunity is null || opportunity.Record?.Metadata is null)
            {
                return new CV_ActionResult
                {
                    ActionId = "BackgroundResearchMissingIsbn",
                    Success = false,
                    Message =
                        "Scout could not refresh the ebook metadata because its working record is no longer available."
                };
            }

            RepairOpportunity currentOpportunity = opportunity;
            var record = currentOpportunity.Record;

            string originalPath =
                record.File?.OriginalFullPath
                ?? string.Empty;

            string fileName =
                record.File?.CurrentName
                ?? "Unknown ebook";

            bool currentAutomaticAuthorization =
                automaticAuthorizationProvider?.Invoke()
                ?? automaticAuthorization;

            Debug.WriteLine(
                $"[ISBN TRACE] COMPLETE START | {fileName} | " +
                $"AutomaticAuthorization={currentAutomaticAuthorization} | " +
                $"ResearchStatus={research.Status} | " +
                $"ResearchCandidates={research.Candidates.Count}");

            FileContext? workingFile = record.File;

            if (workingFile != null)
            {
                E_EbookMetadata? currentMetadata =
                    E_EbookMetadataReader.Read(workingFile);

                if (currentMetadata != null)
                {
                    record.Metadata.Title =
                        currentMetadata.Title;

                    record.Metadata.Author =
                        currentMetadata.Author;

                    record.Metadata.Publisher =
                        currentMetadata.Publisher;

                    record.Metadata.Language =
                        currentMetadata.Language;

                    record.Metadata.Isbn =
                        currentMetadata.Isbn;

                    record.Metadata.Series =
                        currentMetadata.Series;

                    record.Metadata.SeriesNumber =
                        currentMetadata.SeriesNumber;

                    record.Metadata.Description =
                        currentMetadata.Description;

                    record.Metadata.HasCover =
                        currentMetadata.HasCover;

                    record.Metadata.CoverImage =
                        currentMetadata.CoverImage;

                    if (!string.IsNullOrWhiteSpace(
                            currentMetadata.Isbn))
                    {
                        CV_ActionResult alreadyResolved = new()
                        {
                            ActionId = "BackgroundResearchMissingIsbn",
                            Success = true,
                            RequiresReobservation = true,
                            Message =
                                $"Scout re-checked {fileName} and found that its protected working copy already contains ISBN {currentMetadata.Isbn}. The older research result will not be applied again."
                        };

                        alreadyResolved.Evidence.Add(
                            $"Current working EPUB ISBN: {currentMetadata.Isbn}.");

                        return alreadyResolved;
                    }
                }
            }

            if (research.Candidates.Count > 0)
            {
                lock (_researchCacheLock)
                {
                    _researchedIsbnCandidates[originalPath] =
                        research.Candidates.ToList();
                }
            }

            if (research.Diagnostics.Count > 0)
            {
                Debug.WriteLine(
                    "Scout ISBN completion: " +
                    string.Join(" | ", research.Diagnostics));
            }

            if (research.Status == IsbnResearchStatus.TimedOut)
            {
                return CreateBackgroundResearchFailureResult(
                    originalPath,
                    fileName,
                    "The ISBN research sources did not respond within Scout's research time limit. Scout has not treated that as evidence that this ebook has no ISBN.");
            }

            if (research.Status == IsbnResearchStatus.ProviderUnavailable)
            {
                return CreateBackgroundResearchFailureResult(
                    originalPath,
                    fileName,
                    "Scout could not reach the available ISBN research sources. No changes were made and Scout has not treated the connection failure as evidence that this ebook has no ISBN.");
            }

            if (research.Candidates.Count == 0)
            {
                CV_ActionResult result = new()
                {
                    ActionId = "BackgroundResearchMissingIsbn",
                    Success = true,
                    RequiresReobservation = false,
                    Message =
                        $"External ISBN research finished for {fileName}, but no safe ISBN candidate was established."
                };

                foreach (string diagnostic in research.Diagnostics)
                {
                    if (!string.IsNullOrWhiteSpace(diagnostic))
                        result.Evidence.Add(diagnostic);
                }

                AddTerminalRepairOptions(
                    result,
                    originalPath,
                    "No ISBN candidate could be established safely. You can accept the ebook as-is, omit it, or provide additional identifying information.");

                return result;
            }

            List<RepairDecisionCandidate> evaluatedCandidates =
                _repairService.EvaluateIsbnCandidates(
                    opportunity,
                    research.Candidates);

            foreach (RepairDecisionCandidate candidate in evaluatedCandidates)
            {
                Debug.WriteLine(
                    $"[ISBN TRACE] CANDIDATE | {fileName} | " +
                    $"ISBN={candidate.Value} | Confidence={candidate.Confidence:0.000} | " +
                    $"Preferred={candidate.IsPreferred} | Source={candidate.Source}");
            }

            RepairDecisionResult decision =
                _repairDecisionEngine.Evaluate(
                    evaluatedCandidates,
                    E_RepairDecisionEngine.MinimumConfidenceThreshold,
                    automaticAuthorization: currentAutomaticAuthorization);

            Debug.WriteLine(
                $"[ISBN TRACE] DECISION | {fileName} | State={decision.State} | " +
                $"Selected={decision.SelectedCandidate?.Value ?? "<none>"} | " +
                $"DecisionCandidates={decision.Candidates.Count}");

            if (decision.State ==
                    RepairRecommendation.RepairDecisionState.SafeToApply &&
                decision.SelectedCandidate != null &&
                currentAutomaticAuthorization)
            {
                RepairDecisionCandidate selectedCandidate =
                    decision.SelectedCandidate;

                string approvedIsbn =
                    selectedCandidate.Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(approvedIsbn))
                {
                    Debug.WriteLine(
                        $"[ISBN TRACE] APPLY | {fileName} | ISBN={approvedIsbn} | " +
                        $"Confidence={selectedCandidate.Confidence:0.000} | Source={selectedCandidate.Source}");

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

                    string? repairedPath =
                        _repairService.ExecuteRepairPlan(opportunity);

                    Debug.WriteLine(
                        $"[ISBN TRACE] APPLY RESULT | {fileName} | " +
                        $"Success={!string.IsNullOrWhiteSpace(repairedPath)} | Path={repairedPath ?? "<none>"}");

                    if (!string.IsNullOrWhiteSpace(repairedPath))
                    {
                        CV_ActionResult result = new()
                        {
                            ActionId = "BackgroundResearchMissingIsbn",
                            Success = true,
                            RequiresReobservation = true,
                            Message =
                                $"External ISBN research found ISBN {approvedIsbn} for {fileName}, and Scout applied it to the protected working copy. Scout is re-checking the ebook."
                        };

                        result.Evidence.Add(selectedCandidate.Evidence);
                        result.Evidence.Add(
                            $"ISBN candidate confidence: {selectedCandidate.Confidence:0.00}.");

                        foreach (string diagnostic in research.Diagnostics)
                        {
                            if (!string.IsNullOrWhiteSpace(diagnostic))
                                result.Evidence.Add(diagnostic);
                        }

                        return result;
                    }
                }
            }

            Debug.WriteLine(
                $"[ISBN TRACE] UNRESOLVED | {fileName} | State={decision.State} | " +
                $"AutomaticAuthorization={currentAutomaticAuthorization} | " +
                $"Candidates={evaluatedCandidates.Count}");

            CV_ActionResult unresolved = new()
            {
                ActionId = "BackgroundResearchMissingIsbn",
                Success = true,
                RequiresReobservation = false,
                Message =
                    $"Scout researched the ISBN for {fileName}, but the available evidence did not justify an automatic repair."
            };

            foreach (RepairDecisionCandidate candidate in evaluatedCandidates)
            {
                unresolved.Evidence.Add(
                    $"ISBN candidate {candidate.Value}: confidence {candidate.Confidence:0.00}. {candidate.Evidence}");
            }

            foreach (string diagnostic in research.Diagnostics)
            {
                if (!string.IsNullOrWhiteSpace(diagnostic))
                    unresolved.Evidence.Add(diagnostic);
            }

            if (decision.State ==
                RepairRecommendation.RepairDecisionState.UserDecisionRequired)
            {
                foreach (RepairDecisionCandidate candidate in
                         decision.Candidates)
                {
                    unresolved.Options.Add(
                        CreateIsbnActionOption(
                            new CV_ActionRequest
                            {
                                ActionId = "BackgroundResearchMissingIsbn",
                                ContextId = originalPath
                            },
                            originalPath,
                            fileName,
                            candidate,
                            "Use this ISBN"));
                }
            }
            else
            {
                AddTerminalRepairOptions(
                    unresolved,
                    originalPath,
                    "Scout found ISBN evidence, but it was not strong enough for a safe automatic repair. You can accept the ebook as-is, omit it, or provide additional identifying information.");
            }

            return unresolved;
        }

        private static CV_ActionResult CreateBackgroundResearchFailureResult(
            string originalPath,
            string fileName,
            string message)
        {
            CV_ActionResult result = new()
            {
                ActionId = "BackgroundResearchMissingIsbn",
                Success = true,
                RequiresReobservation = false,
                Message = $"External ISBN research for {fileName}: {message}"
            };

            AddTerminalRepairOptions(
                result,
                originalPath,
                message);

            return result;
        }

        private static void AddTerminalRepairOptions(
            CV_ActionResult result,
            string originalPath,
            string evidence)
        {
            result.Evidence.Add(evidence);

            result.Options.Add(
                new CV_ActionOption
                {
                    Id = "AcceptAsIs",
                    ActionId = "AcceptAsIs",
                    ContextId = originalPath,
                    Label = "Accept this ebook as-is",
                    Source = "Ebook Expert"
                });

            result.Options.Add(
                new CV_ActionOption
                {
                    Id = "OmitEbook",
                    ActionId = "OmitEbook",
                    ContextId = originalPath,
                    Label = "Reject / omit this ebook",
                    Source = "Ebook Expert"
                });

            result.Options.Add(
                new CV_ActionOption
                {
                    Id = "AddRepairInformation",
                    ActionId = "AddRepairInformation",
                    ContextId = originalPath,
                    Label = "Add information",
                    Source = "Ebook Expert"
                });
        }

        /// <summary>
        /// Presents the locally evaluated identity candidate for explicit
        /// user approval. No EPUB is modified by this action.
        /// </summary>
        private static CV_ActionResult ReviewMetadataIdentity(
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
                        "I found a metadata identity issue, but I don't know which ebook it belongs to."
                };
            }

            RepairOpportunity? opportunity =
                FindOpportunity(request.ContextId, opportunities);

            if (opportunity?.Record == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I couldn't find the ebook associated with this metadata recovery request."
                };
            }

            // Re-evaluate the current evidence so newly supplied user
            // information can immediately participate in the same recovery
            // path. The original MetadataRecord remains the evidence store.
            E_BookIdentityEvaluator evaluator = new();
            BookIdentityEvaluation evaluation =
                evaluator.Evaluate(opportunity.Record);

            opportunity.IdentityEvaluation = evaluation;

            if (!evaluation.RepairRequired ||
                evaluation.Candidate == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = true,
                    Message =
                        "The current evidence no longer shows a metadata identity repair that needs approval."
                };
            }

            BookIdentityCandidate candidate = evaluation.Candidate;

            bool seriesNeedsMoreEvidence =
                evaluation.SeriesEvaluation?.State == SeriesEvidenceState.Unresolved ||
                evaluation.SeriesEvaluation?.State == SeriesEvidenceState.Conflicting;

            CV_ActionResult result = new()
            {
                ActionId = request.ActionId,
                Success = true,
                Message = seriesNeedsMoreEvidence
                    ? "Scout has evidence about this ebook's identity, but the Series cannot yet be established safely. I need another piece of evidence before changing it."
                    : BuildIdentityCandidateMessage(candidate)
            };

            result.Evidence.AddRange(
                evaluation.Evidence.Where(
                    evidence => !string.IsNullOrWhiteSpace(evidence)));

            if (seriesNeedsMoreEvidence)
            {
                result.Options.Add(
                    new CV_ActionOption
                    {
                        Id = "AddRepairInformation",
                        ActionId = "AddRepairInformation",
                        ContextId = request.ContextId,
                        Label = "Add identifying information",
                        Source = "Ebook Expert"
                    });

                result.Options.Add(
                    CreateUserDecisionOption(
                        request.ContextId,
                        "AcceptAsIs",
                        "Accept this ebook as-is",
                        "The Series could not be established safely from the current evidence."));

                result.Options.Add(
                    CreateUserDecisionOption(
                        request.ContextId,
                        "OmitEbook",
                        "Reject / omit this ebook",
                        "The Series could not be established safely from the current evidence."));

                return result;
            }

            result.Options.Add(
                new CV_ActionOption
                {
                    Id = "ApproveIdentity",
                    ActionId = request.ActionId,
                    ContextId = request.ContextId,
                    Label = BuildIdentityApprovalLabel(candidate),
                    Source = "Ebook Expert"
                });

            return result;
        }

        private static string BuildIdentityCandidateMessage(
            BookIdentityCandidate candidate)
        {
            List<string> parts = new();

            if (!string.IsNullOrWhiteSpace(candidate.Title))
                parts.Add($"Title '{candidate.Title}'");

            if (!string.IsNullOrWhiteSpace(candidate.Authors))
                parts.Add($"Author '{candidate.Authors}'");

            if (!string.IsNullOrWhiteSpace(candidate.Series))
                parts.Add($"Series '{candidate.Series}'");

            if (!string.IsNullOrWhiteSpace(candidate.SeriesNumber))
                parts.Add($"SeriesNumber '{candidate.SeriesNumber}'");

            return parts.Count == 0
                ? "Scout found metadata evidence that needs reconciliation."
                : "Scout found a metadata correction: " + string.Join("; ", parts) + ".";
        }

        private static string BuildIdentityApprovalLabel(
            BookIdentityCandidate candidate)
        {
            List<string> parts = new();

            if (!string.IsNullOrWhiteSpace(candidate.Title))
                parts.Add($"Title '{candidate.Title}'");

            if (!string.IsNullOrWhiteSpace(candidate.Authors))
                parts.Add($"Author '{candidate.Authors}'");

            if (!string.IsNullOrWhiteSpace(candidate.Series))
                parts.Add($"Series '{candidate.Series}'");

            if (!string.IsNullOrWhiteSpace(candidate.SeriesNumber))
                parts.Add($"# {candidate.SeriesNumber}");

            return "Use " + string.Join(" / ", parts);
        }

        /// <summary>
        /// Converts an explicitly approved local identity candidate into the
        /// ordinary repair changes already understood by E_RepairPlan.
        ///
        /// Series and SeriesNumber are created only when the evidence evaluator
        /// has established them as local candidates. Unresolved or conflicting
        /// Series evidence is routed back to evidence gathering instead of being
        /// silently written.
        /// </summary>
        private CV_ActionResult ApproveIdentityCandidate(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            lock (_repairExecutionLock)
            {
                return ApproveIdentityCandidateCore(
                    request,
                    opportunities);
            }
        }

        private CV_ActionResult ApproveIdentityCandidateCore(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            if (!string.Equals(
                    request.OptionId,
                    "ApproveIdentity",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message = "I couldn't verify that identity approval."
                };
            }

            if (string.IsNullOrWhiteSpace(request.ContextId))
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I received the identity approval, but I don't know which ebook it belongs to."
                };
            }

            RepairOpportunity? opportunity =
                FindOpportunity(request.ContextId, opportunities);

            if (opportunity?.Record?.Metadata == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I couldn't verify the current identity candidate for this ebook."
                };
            }

            E_BookIdentityEvaluator evaluator = new();
            BookIdentityEvaluation evaluation =
                evaluator.Evaluate(opportunity.Record);

            opportunity.IdentityEvaluation = evaluation;

            if (evaluation.RepairRequired != true ||
                evaluation.Candidate == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "The current evidence no longer supports an approved identity correction."
                };
            }

            if (evaluation.SeriesEvaluation?.State == SeriesEvidenceState.Unresolved ||
                evaluation.SeriesEvaluation?.State == SeriesEvidenceState.Conflicting)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "The Series is still unresolved. Scout needs additional evidence before it can approve this identity correction."
                };
            }

            BookIdentityCandidate candidate = evaluation.Candidate;
            string evidence = string.Join(
                Environment.NewLine,
                evaluation.Evidence.Where(
                    item => !string.IsNullOrWhiteSpace(item)));

            if (string.IsNullOrWhiteSpace(evidence))
                evidence = evaluation.Reason;

            const string source = "UserApprovedIdentity";
            const double approvedConfidence = 1.0;

            int changesAdded = 0;

            if (!string.IsNullOrWhiteSpace(candidate.Title) &&
                !string.Equals(
                    opportunity.Record.Metadata.Title,
                    candidate.Title,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    request.ContextId,
                    new E_RepairChange(
                        "Title",
                        opportunity.Record.Metadata.Title,
                        candidate.Title,
                        source,
                        evidence,
                        approvedConfidence,
                        true));

                changesAdded++;
            }

            if (!string.IsNullOrWhiteSpace(candidate.Authors) &&
                !string.Equals(
                    opportunity.Record.Metadata.Author,
                    candidate.Authors,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    request.ContextId,
                    new E_RepairChange(
                        "Author",
                        opportunity.Record.Metadata.Author,
                        candidate.Authors,
                        source,
                        evidence,
                        approvedConfidence,
                        true));

                changesAdded++;
            }

            if (!string.IsNullOrWhiteSpace(candidate.Series) &&
                !string.Equals(
                    opportunity.Record.Metadata.Series,
                    candidate.Series,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    request.ContextId,
                    new E_RepairChange(
                        "Series",
                        opportunity.Record.Metadata.Series,
                        candidate.Series,
                        source,
                        evidence,
                        approvedConfidence,
                        true));

                changesAdded++;
            }

            if (!string.IsNullOrWhiteSpace(candidate.SeriesNumber) &&
                !string.Equals(
                    opportunity.Record.Metadata.SeriesNumber,
                    candidate.SeriesNumber,
                    StringComparison.Ordinal))
            {
                _repairService.AddRepairChange(
                    request.ContextId,
                    new E_RepairChange(
                        "SeriesNumber",
                        opportunity.Record.Metadata.SeriesNumber,
                        candidate.SeriesNumber,
                        source,
                        evidence,
                        approvedConfidence,
                        true));

                changesAdded++;
            }

            if (changesAdded == 0)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "The approved identity does not contain a new metadata value to repair."
                };
            }

            CV_ActionResult result = new()
            {
                ActionId = request.ActionId,
                Success = true,
                Message =
                    "The identity correction has been approved and added to the repair plan. " +
                    "The original EPUB has not been modified."
            };

            result.Evidence.Add(
                $"Approved identity: {candidate.Title} — {candidate.Authors}");

            result.Options.Add(
                new CV_ActionOption
                {
                    Id = "ApplyRepair",
                    ActionId = "ExecuteRepairPlan",
                    ContextId = request.ContextId,
                    Label = "Click to apply this identity repair",
                    Confidence = 1.0,
                    Source = "Ebook Expert"
                });

            return result;
        }

        /// <summary>
        /// Presents the human decision boundary for repair facts that the
        /// Ebook Expert can detect but cannot currently research or repair.
        /// No EPUB is modified by this action.
        /// </summary>
        private static CV_ActionResult ReviewUnsupportedRepair(
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
                        "I found a repair need that Scout cannot currently recover, but I don't know which ebook it belongs to."
                };
            }

            RepairOpportunity? opportunity =
                FindOpportunity(request.ContextId, opportunities);

            if (opportunity?.Record == null)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = false,
                    Message =
                        "I couldn't find the ebook associated with this repair boundary."
                };
            }

            string fileName =
                opportunity.Record.File.CurrentName
                ?? opportunity.Record.File.OriginalName
                ?? request.ContextId;

            List<string> missingFields = new();

            if (opportunity.MissingTitle &&
                opportunity.IdentityEvaluation?.RepairRequired != true)
                missingFields.Add("Title");

            if (opportunity.MissingAuthor &&
                opportunity.IdentityEvaluation?.RepairRequired != true)
                missingFields.Add("Author");

            // Publisher and Description now have a background research path.
            // Only fields without a completed external recovery capability
            // belong in the unsupported bucket.
            if (opportunity.MissingLanguage)
                missingFields.Add("Language");

            if (opportunity.MissingCover)
                missingFields.Add("Cover");

            bool needsMetadataResearch =
                opportunity.MissingPublisher || opportunity.MissingDescription;

            CV_ActionResult result = new()
            {
                ActionId = request.ActionId,
                Success = true,
                Message =
                    needsMetadataResearch
                        ? missingFields.Count == 0
                            ? $"Scout is researching Publisher and Summary information for {fileName} in the background."
                            : $"Scout can research Publisher and Summary information for {fileName}. The remaining unsupported fields are: {string.Join(", ", missingFields)}."
                        : missingFields.Count == 0
                            ? $"Scout no longer sees an unsupported repair need for {fileName}."
                            : $"Scout found these fields that still need another recovery path for {fileName}: {string.Join(", ", missingFields)}."
            };

            foreach (string field in missingFields)
            {
                result.Evidence.Add(
                    $"Unsupported repair field: {field}.");
            }

            if (opportunity.MissingPublisher || opportunity.MissingDescription)
            {
                result.Options.Add(
                    new CV_ActionOption
                    {
                        Id = "ResearchMissingMetadata",
                        ActionId = "ResearchMissingMetadata",
                        ContextId = request.ContextId,
                        Label = "Research Publisher / Summary",
                        Source = "Ebook Expert"
                    });
            }

            if (missingFields.Count > 0)
            {
                result.Evidence.Add(
                    "The original EPUB has not been modified by this action.");

                result.Options.Add(
                    CreateUserDecisionOption(
                        request.ContextId,
                        "AcceptAsIs",
                        "Accept this ebook as-is",
                        "You are choosing to organize the current ebook without repairing the unsupported fields."));

                result.Options.Add(
                    CreateUserDecisionOption(
                        request.ContextId,
                        "OmitEbook",
                        "Reject / omit this ebook",
                        "You are choosing not to include this ebook in Organization."));

                result.Options.Add(
                    CreateUserDecisionOption(
                        request.ContextId,
                        "AddRepairInformation",
                        "Provide missing information",
                        "Provide a value for the missing text metadata and Scout will send it through the normal repair path."));
            }

            return result;
        }

        private static RepairOpportunity? FindOpportunity(
            string originalPath,
            IReadOnlyList<RepairOpportunity> opportunities)
        {
            foreach (RepairOpportunity opportunity in opportunities)
            {
                string path =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                if (string.Equals(
                    path,
                    originalPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return opportunity;
                }
            }

            return null;
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
            lock (_repairExecutionLock)
            {
                return SelectIsbnCandidateCore(
                    request,
                    opportunities);
            }
        }

        private CV_ActionResult SelectIsbnCandidateCore(
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

            List<IsbnResearchCandidate> candidates;

            lock (_researchCacheLock)
            {
                candidates =
                    _researchedIsbnCandidates.TryGetValue(
                        request.ContextId,
                        out List<IsbnResearchCandidate>? cachedCandidates)
                        ? cachedCandidates.ToList()
                        : new List<IsbnResearchCandidate>();
            }

            // A candidate normally comes from the background research that
            // produced the option. If no cached result exists, retain the
            // existing validation fallback.
            if (candidates.Count == 0)
            {
                E_BookIdentityEvaluator identityEvaluator = new();
                BookIdentityEvaluation identityEvaluation =
                    identityEvaluator.Evaluate(selectedOpportunity.Record);

                selectedOpportunity.IdentityEvaluation = identityEvaluation;

                if (!IsIdentityReadyForExternalResearch(
                        selectedOpportunity,
                        identityEvaluation))
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message =
                            "Scout could not validate an ISBN selection because the ebook's local Title/Author identity still needs reconciliation."
                    };
                }

                candidates =
                    _repairService.ResearchMissingIsbn(
                        selectedOpportunity,
                        null,
                        identityEvaluation);
            }

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
        private CV_ActionResult QueueManualIsbnResearch(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities,
            bool automaticAuthorization,
            string? userEvidence,
            Func<bool>? automaticAuthorizationProvider,
            Func<string, RepairOpportunity?>? currentOpportunityResolver)
        {
            int queued = 0;
            int blocked = 0;

            foreach (RepairOpportunity opportunity in opportunities)
            {
                if (!opportunity.MissingIsbn)
                    continue;

                string originalPath =
                    opportunity.Record?.File?.OriginalFullPath
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(originalPath))
                    continue;

                if (!string.IsNullOrWhiteSpace(request.ContextId) &&
                    !string.Equals(
                        originalPath,
                        request.ContextId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                E_BookIdentityEvaluator identityEvaluator = new();
                BookIdentityEvaluation identityEvaluation =
                    identityEvaluator.Evaluate(opportunity.Record);

                opportunity.IdentityEvaluation = identityEvaluation;

                if (!IsIdentityReadyForExternalResearch(
                        opportunity,
                        identityEvaluation))
                {
                    blocked++;
                    continue;
                }

                lock (_externalResearchingPaths)
                {
                    _externalResearchingPaths.Add(originalPath);
                }

                bool accepted =
                    _externalResearchCoordinator.Enqueue(
                        originalPath,
                        cancellationToken =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            RepairOpportunity? currentOpportunity =
                                currentOpportunityResolver?.Invoke(
                                    originalPath)
                                ?? opportunity;

                            if (currentOpportunity == null ||
                                !currentOpportunity.MissingIsbn)
                            {
                                return Task.FromResult(
                                    new CV_ActionResult
                                    {
                                        ActionId = "BackgroundResearchMissingIsbn",
                                        Success = true,
                                        RequiresReobservation = false,
                                        Message =
                                            "This ebook no longer needs the queued ISBN research, so Scout did not perform another external lookup."
                                    });
                            }

                            E_BookIdentityEvaluator currentIdentityEvaluator =
                                new();

                            BookIdentityEvaluation currentIdentityEvaluation =
                                currentIdentityEvaluator.Evaluate(
                                    currentOpportunity.Record);

                            currentOpportunity.IdentityEvaluation =
                                currentIdentityEvaluation;

                            if (!IsIdentityReadyForExternalResearch(
                                    currentOpportunity,
                                    currentIdentityEvaluation))
                            {
                                return Task.FromResult(
                                    new CV_ActionResult
                                    {
                                        ActionId = "BackgroundResearchMissingIsbn",
                                        Success = true,
                                        RequiresReobservation = false,
                                        Message =
                                            "Scout did not perform external ISBN research because the ebook's local Title/Author identity still needs reconciliation."
                                    });
                            }

                            IsbnResearchResult research =
                                _repairService.ResearchMissingIsbnWithStatus(
                                    currentOpportunity,
                                    userEvidence,
                                    currentIdentityEvaluation);

                            cancellationToken.ThrowIfCancellationRequested();

                            return Task.FromResult(
                                CompleteAutomaticIsbnResearch(
                                    currentOpportunity,
                                    research,
                                    automaticAuthorization,
                                    automaticAuthorizationProvider));
                        },
                        result =>
                        {
                            lock (_externalResearchingPaths)
                            {
                                _externalResearchingPaths.Remove(originalPath);
                            }

                            BackgroundActionCompleted?.Invoke(
                                originalPath,
                                result);
                        });

                if (accepted)
                    queued++;
                else
                {
                    lock (_externalResearchingPaths)
                    {
                        _externalResearchingPaths.Remove(originalPath);
                    }
                }
            }

            if (queued == 0)
            {
                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = blocked > 0,
                    RequiresReobservation = false,
                    Message = blocked > 0
                        ? $"Scout did not perform external ISBN research for {blocked:N0} ebook(s) because their local Title/Author identity still needs reconciliation. I will research the ISBN after that identity is established."
                        : "I couldn't find an active ebook that needs ISBN research."
                };
            }

            return new CV_ActionResult
            {
                ActionId = request.ActionId,
                Success = true,
                RequiresReobservation = false,
                Message = blocked > 0
                    ? $"I'm researching ISBNs for {queued:N0} ebook(s) whose local identity is established. I held back {blocked:N0} ebook(s) until their Title/Author identity is reconciled."
                    : queued == 1
                        ? "I'm researching the ISBN in the background while Scout continues processing the collection."
                        : $"I'm researching ISBNs for {queued:N0} ebooks in the background while Scout continues processing the collection."
            };
        }

        private CV_ActionResult ResearchMissingIsbn(
            CV_ActionRequest request,
            IReadOnlyList<RepairOpportunity> opportunities,
            bool automaticAuthorization,
            string? userEvidence = null,
            Func<bool>? automaticAuthorizationProvider = null,
            Func<string, RepairOpportunity?>? currentOpportunityResolver = null)
        {
            //---------------------------------------------------------
            // External ISBN research is always background work. The
            // conversation action returns immediately and the eventual
            // result comes back through BackgroundActionCompleted.
            // A request with an OptionId is handled by SelectIsbnCandidate
            // before this method and therefore remains a selection action.
            //---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(request.OptionId))
            {
                return QueueManualIsbnResearch(
                    request,
                    opportunities,
                    automaticAuthorization,
                    userEvidence,
                    automaticAuthorizationProvider,
                    currentOpportunityResolver);
            }

            List<string> evidence = [];
            List<CV_ActionOption> options = [];

            int researchedBooks = 0;
            int candidateCount = 0;
            bool repairExecuted = false;

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

                E_BookIdentityEvaluator identityEvaluator = new();
                BookIdentityEvaluation identityEvaluation =
                    identityEvaluator.Evaluate(opportunity.Record);

                opportunity.IdentityEvaluation = identityEvaluation;

                if (!IsIdentityReadyForExternalResearch(
                        opportunity,
                        identityEvaluation))
                {
                    evidence.Add(
                        $"{opportunity.Record?.File?.CurrentName ?? "Unknown ebook"}: Scout will not research an ISBN externally until the local Title/Author identity is reconciled.");
                    continue;
                }

                researchedBooks++;

                List<IsbnResearchCandidate> candidates =
                    _repairService.ResearchMissingIsbn(
                        opportunity,
                        request.UserInput,
                        identityEvaluation);

                string fileName =
                    opportunity.Record?.File?.CurrentName
                    ?? "Unknown ebook";

                if (candidates.Count == 0)
                {
                    evidence.Add(
                        $"{fileName}: Scout could not find a usable ISBN candidate.");

                    options.Add(
                        CreateUserDecisionOption(
                            originalPath,
                            "AcceptAsIs",
                            "Accept this ebook as-is",
                            "No ISBN candidate could be established safely. You are choosing to organize the current ebook without repairing this ISBN."));

                    options.Add(
                        CreateUserDecisionOption(
                            originalPath,
                            "OmitEbook",
                            "Reject / omit this ebook",
                            "No ISBN candidate could be established safely. You are choosing not to include this ebook in Organization."));

                    options.Add(
                        CreateUserDecisionOption(
                            originalPath,
                            "AddRepairInformation",
                            "Add information",
                            "No ISBN candidate could be established safely. Provide additional identifying information, then ask Scout to research the ISBN again."));

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
                        candidates,
                        userEvidence);

                RepairDecisionResult decision =
                    _repairDecisionEngine.Evaluate(
                        evaluatedCandidates,
                        E_RepairDecisionEngine.MinimumConfidenceThreshold,
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

                    Debug.WriteLine(
                        $"[ISBN TRACE] APPLY | {fileName} | ISBN={approvedIsbn} | " +
                        $"Confidence={selectedCandidate.Confidence:0.000} | Source={selectedCandidate.Source}");

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

                    string? repairedPath =
                        _repairService.ExecuteRepairPlan(opportunity);

                    Debug.WriteLine(
                        $"[ISBN TRACE] APPLY RESULT | {fileName} | " +
                        $"Success={!string.IsNullOrWhiteSpace(repairedPath)} | Path={repairedPath ?? "<none>"}");

                    if (!string.IsNullOrWhiteSpace(repairedPath))
                    {
                        repairExecuted = true;

                        evidence.Add(
                            $"{fileName}: Scout authorized and selected ISBN " +
                            $"{approvedIsbn} for automatic repair.");

                        evidence.Add(
                            $"{fileName}: {selectedCandidate.Evidence}");

                        evidence.Add(
                            $"{fileName}: repair applied successfully; " +
                            "the EPUB will be re-observed.");
                    }
                    else
                    {
                        evidence.Add(
                            $"{fileName}: Scout selected ISBN {approvedIsbn}, " +
                            "but the physical repair did not complete.");

                        options.Add(
                            CreateUserDecisionOption(
                                originalPath,
                                "AcceptAsIs",
                                "Accept this ebook as-is",
                                "Scout could not complete the physical ISBN repair. " +
                                "You are choosing to organize the current ebook without " +
                                "repairing this ISBN."));

                        options.Add(
                            CreateUserDecisionOption(
                                originalPath,
                                "OmitEbook",
                                "Reject / omit this ebook",
                                "Scout could not complete the physical ISBN repair. " +
                                "You are choosing not to include this ebook in Organization."));

                        options.Add(
                            CreateUserDecisionOption(
                                originalPath,
                                "AddRepairInformation",
                                "Add information",
                                "Scout could not complete the physical ISBN repair. " +
                                "Provide additional information, then ask Scout to research " +
                                "the ISBN again."));
                    }
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
                    // The available evidence is not sufficient for a repair.
                    //
                    // Do not present unsupported candidates as choices.
                    //---------------------------------------------------------

                    evidence.Add(
                        $"{fileName}: there was not enough evidence to " +
                        "select an ISBN safely.");

                    options.Add(
                        CreateUserDecisionOption(
                            originalPath,
                            "AcceptAsIs",
                            "Accept this ebook as-is",
                            "You are choosing to organize the current ebook without repairing this ISBN."));

                    options.Add(
                        CreateUserDecisionOption(
                            originalPath,
                            "OmitEbook",
                            "Reject / omit this ebook",
                            "You are choosing not to include this ebook in Organization."));

                    options.Add(
                        CreateUserDecisionOption(
                            originalPath,
                            "AddRepairInformation",
                            "Add information",
                            "Provide additional identifying information, then ask Scout to research the ISBN again."));
                }

                evidence.Add(
                    $"{fileName}: found {evaluatedCandidates.Count} ISBN candidate(s) after EPUB evidence evaluation.");


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
                Message =
        $"ISBN research completed for {researchedBooks} ebook(s). " +
        $"{candidateCount} candidate(s) were found."
            };

            result.Evidence.AddRange(evidence);
            result.Options.AddRange(options);

            //---------------------------------------------------------
            // A successful automatic repair changes the EPUB's current
            // file state. ProjectWorkflow will respond by re-observing it.
            //---------------------------------------------------------


            return result;
        }
    }
}