using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SmartRenamer.Models;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.BuildingBlocks;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;
using Scout.Observations.Experts.EbookExpert.Investigations.Repair;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// E_RepairService
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates ebook repair operations within the Ebook Expert.
    ///
    /// The Service is the domain-level bridge between RepairOpportunity objects
    /// and the Resources that perform research and EPUB modification.
    ///
    /// Current Capabilities
    /// -------------------------------------------------------------------------
    /// • Research a missing ISBN.
    /// • Evaluate ISBN research using additional EPUB content evidence.
    /// • Prepare an approved ISBN in a temporary working copy.
    /// • Verify an ISBN after repair.
    ///
    /// Safety Boundary
    /// -------------------------------------------------------------------------
    /// Research never modifies an ebook.
    ///
    /// Repair never modifies the original ebook.
    ///
    /// Approved repairs are performed against a temporary Ebook Expert
    /// working copy. The prepared copy can later be handed to Scout's
    /// organization process.
    ///
    /// This Service does NOT
    /// -------------------------------------------------------------------------
    /// • Decide whether an ebook needs repair.
    /// • Decide which recommendation Scout should present.
    /// • Interpret user conversation.
    /// • Select an ISBN automatically.
    /// • Automatically approve a repair.
    /// • Decide where the final organized copy belongs.
    ///
    /// Those responsibilities belong to the Repair Investigation,
    /// Conversation Framework, user, and Scout organization workflow.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_RepairService
    {
        private readonly E_IsbnResearchResource _isbnResearchResource = new();

        private readonly E_IsbnRepairEvidenceEvaluator
            _isbnRepairEvidenceEvaluator = new();

        private readonly E_EpubRepairResource _epubRepairResource = new();

        private readonly E_RepairWorkspace _repairWorkspace = new();

        //---------------------------------------------------------
        // Prepared working copies
        //---------------------------------------------------------
        //
        // Maps the original ebook path to the temporary repaired copy.
        //
        // The original path remains the stable identity of the ebook.
        //
        //---------------------------------------------------------

        private readonly Dictionary<string, string> _preparedFiles =
            new(StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------
        // Repair Plans
        //---------------------------------------------------------
        //
        // Each source EPUB owns ONE repair plan.
        //
        // Approved repairs are accumulated here before any
        // physical EPUB repair is performed.
        //
        // This allows one ebook to receive multiple repairs:
        //
        //     ISBN
        //     Description
        //     Cover
        //     Publisher
        //     etc.
        //
        // and later have ALL approved changes applied to ONE
        // working copy.
        //
        // The original EPUB remains untouched.
        //---------------------------------------------------------

        private readonly Dictionary<string, E_RepairPlan> _repairPlans =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Recognizes ISBN values directly from the EPUB opening content.
        ///
        /// This is local evidence from the EPUB itself. No external research
        /// is performed, and no repair is applied by this operation.
        /// </summary>
        public List<IsbnResearchCandidate> RecognizeIsbnCandidates(
            RepairOpportunity opportunity,
            int maxDocuments = 10)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (opportunity.Record?.Metadata == null)
                return new List<IsbnResearchCandidate>();

            string epubPath =
                opportunity.Record.File?.CurrentFullPath
                ?? opportunity.Record.File?.OriginalFullPath
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(epubPath))
                return new List<IsbnResearchCandidate>();

            string openingContent =
                new E_EpubContentResource().ExtractOpeningText(
                    epubPath,
                    maxDocuments);

            IReadOnlyList<string> recognizedIsbns =
                new E_IsbnContentRecognizer().Recognize(openingContent);

            return recognizedIsbns
                .Select(isbn => new IsbnResearchCandidate
                {
                    Isbn = isbn,
                    Source = "LocalEpubContent",
                    Evidence =
                        "ISBN was explicitly found in the EPUB opening content.",
                    Confidence = 1.0
                })
                .ToList();
        }

        /// <summary>
        /// Researches possible ISBN values for a repair opportunity.
        ///
        /// No ebook is modified by this operation.
        /// </summary>
        public List<IsbnResearchCandidate> ResearchMissingIsbn(
            RepairOpportunity opportunity,
            string? userEvidence = null,
            BookIdentityEvaluation? identityEvaluation = null)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            //---------------------------------------------------------
            // Research is only appropriate when the Repair Block
            // identified a missing ISBN.
            //---------------------------------------------------------

            if (!opportunity.MissingIsbn)
                return new List<IsbnResearchCandidate>();

            //---------------------------------------------------------
            // The RepairOpportunity contains the metadata record
            // discovered by the Repair Block.
            //---------------------------------------------------------

            if (opportunity.Record?.Metadata == null)
                return new List<IsbnResearchCandidate>();

            string combinedUserEvidence =
                CombineUserEvidence(
                    opportunity.Record.Evidence,
                    userEvidence);

            E_EbookMetadata researchMetadata =
                BuildResearchMetadata(
                    opportunity.Record.Metadata,
                    identityEvaluation);

            Debug.WriteLine(
                $"[IDENTITY TRACE] ISBN RESEARCH IDENTITY | " +
                $"ObservedTitle='{opportunity.Record.Metadata.Title}' | " +
                $"ObservedAuthor='{opportunity.Record.Metadata.Author}' | " +
                $"QueryTitle='{researchMetadata.Title}' | " +
                $"QueryAuthor='{researchMetadata.Author}' | " +
                $"Series='{researchMetadata.Series}' | " +
                $"IdentityRepairRequired={identityEvaluation?.RepairRequired ?? false}");

            return _isbnResearchResource.Research(
                researchMetadata,
                combinedUserEvidence,
                opportunity.Record.Evidence);
        }

        /// <summary>
        /// Researches a missing ISBN while preserving the external provider
        /// outcome for the background recovery coordinator.
        /// </summary>
        public IsbnResearchResult ResearchMissingIsbnWithStatus(
            RepairOpportunity opportunity,
            string? userEvidence = null,
            BookIdentityEvaluation? identityEvaluation = null)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (!opportunity.MissingIsbn)
            {
                return new IsbnResearchResult
                {
                    Status = IsbnResearchStatus.NoCandidates
                };
            }

            if (opportunity.Record?.Metadata == null)
            {
                return new IsbnResearchResult
                {
                    Status = IsbnResearchStatus.NoCandidates
                };
            }

            IReadOnlyList<MetadataEvidence> evidence =
                opportunity.Record.Evidence;

            string combinedUserEvidence =
                CombineUserEvidence(
                    opportunity.Record.Evidence,
                    userEvidence);

            E_EbookMetadata researchMetadata =
                BuildResearchMetadata(
                    opportunity.Record.Metadata,
                    identityEvaluation);

            Debug.WriteLine(
                $"[IDENTITY TRACE] ISBN RESEARCH IDENTITY | " +
                $"ObservedTitle='{opportunity.Record.Metadata.Title}' | " +
                $"ObservedAuthor='{opportunity.Record.Metadata.Author}' | " +
                $"QueryTitle='{researchMetadata.Title}' | " +
                $"QueryAuthor='{researchMetadata.Author}' | " +
                $"Series='{researchMetadata.Series}' | " +
                $"IdentityRepairRequired={identityEvaluation?.RepairRequired ?? false}");

            return _isbnResearchResource.ResearchWithStatus(
                researchMetadata,
                combinedUserEvidence,
                evidence);
        }

        /// <summary>
        /// Creates the identity view used to construct an external research
        /// query. This is deliberately NOT written back to the EPUB and does
        /// not become observed metadata. It is the local expert's reconciled
        /// query identity.
        /// </summary>
        private static E_EbookMetadata BuildResearchMetadata(
            E_EbookMetadata observedMetadata,
            BookIdentityEvaluation? identityEvaluation)
        {
            E_EbookMetadata researchMetadata = new()
            {
                Title = observedMetadata.Title,
                Author = observedMetadata.Author,
                Publisher = observedMetadata.Publisher,
                Language = observedMetadata.Language,
                Isbn = observedMetadata.Isbn,
                Series = observedMetadata.Series,
                SeriesNumber = observedMetadata.SeriesNumber,
                Description = observedMetadata.Description,
                HasCover = observedMetadata.HasCover,
                CoverImage = observedMetadata.CoverImage
            };

            BookIdentityCandidate? candidate = identityEvaluation?.Candidate;

            if (candidate == null)
                return researchMetadata;

            // Title and Author are the core research identity. Use the local
            // expert's reconciled values when it has established them. This
            // does not modify the observed EPUB and is never treated as fresh
            // evidence; the provider result is still evaluated against the
            // EPUB evidence before a repair decision is made.
            if (!string.IsNullOrWhiteSpace(candidate.Title))
                researchMetadata.Title = candidate.Title;

            if (!string.IsNullOrWhiteSpace(candidate.Authors))
                researchMetadata.Author = candidate.Authors;

            if (identityEvaluation?.SeriesEvaluation?.State ==
                SeriesEvidenceState.Resolved)
            {
                if (!string.IsNullOrWhiteSpace(candidate.Series))
                    researchMetadata.Series = candidate.Series;

                if (!string.IsNullOrWhiteSpace(candidate.SeriesNumber))
                    researchMetadata.SeriesNumber = candidate.SeriesNumber;
            }

            return researchMetadata;
        }

        /// <summary>
        /// Evaluates researched ISBN candidates using additional evidence
        /// found inside the EPUB itself.
        ///
        /// This operation does not select, approve, or apply a repair.
        /// It produces generic repair decision candidates for the
        /// E_RepairDecisionEngine.
        /// </summary>
        public List<RepairDecisionCandidate> EvaluateIsbnCandidates(
            RepairOpportunity opportunity,
            IReadOnlyList<IsbnResearchCandidate> candidates,
            string? userEvidence = null,
            int maxDocuments = 10)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            if (opportunity.Record?.Metadata == null)
                return new List<RepairDecisionCandidate>();

            if (candidates.Count == 0)
                return new List<RepairDecisionCandidate>();

            //---------------------------------------------------------
            // The current EPUB path is the file that should be examined.
            //
            // CurrentFullPath may point to a repaired working copy.
            // OriginalFullPath remains the stable identity of the EPUB.
            //---------------------------------------------------------

            string epubPath =
                opportunity.Record.File?.CurrentFullPath
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(epubPath))
            {
                epubPath =
                    opportunity.Record.File?.OriginalFullPath
                    ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(epubPath))
                return new List<RepairDecisionCandidate>();

            //---------------------------------------------------------
            // Let the ISBN-specific evidence evaluator interpret the
            // research candidates against the actual EPUB content.
            //
            // The evaluator remains responsible for deciding what
            // constitutes supporting ISBN evidence.
            //---------------------------------------------------------

            string combinedUserEvidence =
                CombineUserEvidence(
                    opportunity.Record.Evidence,
                    userEvidence);

            return _isbnRepairEvidenceEvaluator.Evaluate(
                opportunity.Record.Metadata,
                opportunity.Record.Evidence,
                epubPath,
                candidates,
                combinedUserEvidence,
                maxDocuments);
        }

        private static string CombineUserEvidence(
            IReadOnlyList<MetadataEvidence> recordedEvidence,
            string? currentUserEvidence)
        {
            List<string> values = new();

            foreach (MetadataEvidence evidence in recordedEvidence)
            {
                if (!string.Equals(
                        evidence.Source,
                        "User",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(evidence.Value))
                    values.Add(evidence.Value.Trim());
            }

            if (!string.IsNullOrWhiteSpace(currentUserEvidence))
                values.Add(currentUserEvidence.Trim());

            return string.Join(
                " | ",
                values.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds one approved repair change to the repair plan belonging
        /// to the specified EPUB.
        ///
        /// No EPUB is created or modified by this operation.
        ///
        /// Multiple approved changes for the same EPUB accumulate in the
        /// same repair plan.
        /// </summary>
        public void AddRepairChange(
            string originalPath,
            E_RepairChange change)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                throw new ArgumentException(
                    "Original EPUB path cannot be empty.",
                    nameof(originalPath));

            if (change == null)
                throw new ArgumentNullException(nameof(change));

            //---------------------------------------------------------
            // Get the existing plan for this EPUB or create one.
            //---------------------------------------------------------

            if (!_repairPlans.TryGetValue(
                    originalPath,
                    out E_RepairPlan? repairPlan))
            {
                repairPlan = new E_RepairPlan(
                    originalPath);

                _repairPlans[originalPath] =
                    repairPlan;
            }

            //---------------------------------------------------------
            // Replace an existing change for the same repair type.
            //
            // Approval actions may be repeated by the conversation layer.
            // Keeping one current approved change per field prevents stale
            // duplicate changes from accumulating in the plan.
            //---------------------------------------------------------

            int existingIndex =
                repairPlan.Changes.FindIndex(
                    existing => string.Equals(
                        existing.RepairType,
                        change.RepairType,
                        StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                repairPlan.Changes[existingIndex] = change;
                return;
            }

            repairPlan.AddChange(change);
        }

        /// <summary>
        /// Returns the complete repair plan for one original EPUB.
        ///
        /// The plan contains every repair explicitly approved by the user.
        /// Returns null when no repairs have been approved.
        /// </summary>
        public E_RepairPlan? GetRepairPlan(
            string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                return null;

            return _repairPlans.TryGetValue(
                originalPath,
                out E_RepairPlan? repairPlan)
                ? repairPlan
                : null;
        }

        /// <summary>
        /// Executes all currently approved repairs for one EPUB.
        ///
        /// Creates ONE working copy from the original EPUB, applies every
        /// executable repair in the plan, verifies the resulting EPUB,
        /// and preserves the completed working copy for later handoff.
        ///
        /// The original EPUB is never modified.
        /// </summary>
        public string? ExecuteRepairPlan(
            RepairOpportunity opportunity)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (opportunity.Record?.File == null)
                return null;

            string originalPath =
                opportunity.Record.File.OriginalFullPath;

            if (string.IsNullOrWhiteSpace(originalPath))
            {
                originalPath =
                    opportunity.Record.File.CurrentFullPath;
            }

            if (string.IsNullOrWhiteSpace(originalPath))
                return null;

            E_RepairPlan? repairPlan =
                GetRepairPlan(originalPath);

            if (repairPlan == null || repairPlan.Changes.Count == 0)
                return null;

            //---------------------------------------------------------
            // Create ONE protected working representation. The current
            // working representation is not promoted until every approved
            // change succeeds.
            //---------------------------------------------------------

            string workingPath =
                _repairWorkspace.CreateWorkingCopy(
                    opportunity.Record.File);

            foreach (E_RepairChange change in repairPlan.Changes)
            {
                if (!change.CanExecute)
                {
                    TryDeleteWorkingCopy(workingPath);
                    return null;
                }

                bool repaired =
                    _epubRepairResource.ApplyRepairChange(
                        opportunity.Record.File,
                        change,
                        workingPath);

                if (!repaired)
                {
                    // Earlier successful changes may already exist in the
                    // temporary working representation. Never promote a
                    // partial plan.
                    TryDeleteWorkingCopy(workingPath);
                    return null;
                }
            }

            if (!File.Exists(workingPath))
            {
                TryDeleteWorkingCopy(workingPath);
                return null;
            }

            opportunity.Record.File.CurrentFullPath =
                workingPath;

            opportunity.Record.File.CurrentName =
                Path.GetFileName(workingPath);

            _preparedFiles[originalPath] =
                workingPath;

            repairPlan.Changes.Clear();

            return workingPath;
        }

        private static void TryDeleteWorkingCopy(
            string workingPath)
        {
            if (string.IsNullOrWhiteSpace(workingPath) ||
                !File.Exists(workingPath))
            {
                return;
            }

            try
            {
                File.Delete(workingPath);
            }
            catch
            {
                // Best-effort cleanup. A failed working copy must never be
                // promoted to the current ebook representation.
            }
        }

    }
}