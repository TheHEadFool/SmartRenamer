using System;
using System.Collections.Generic;
using System.IO;
using SmartRenamer.Models;
using SmartRenamer.Observations.BuildingBlocks;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;

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
        /// Researches possible ISBN values for a repair opportunity.
        ///
        /// No ebook is modified by this operation.
        /// </summary>
        public List<IsbnResearchCandidate> ResearchMissingIsbn(
            RepairOpportunity opportunity)
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

            return _isbnResearchResource.Research(
                opportunity.Record.Metadata);
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
            // Add the approved repair to this EPUB's plan.
            //---------------------------------------------------------

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

            //---------------------------------------------------------
            // The original EPUB path is the stable identity of the plan.
            //---------------------------------------------------------

            string originalPath =
                opportunity.Record.File.OriginalFullPath;

            if (string.IsNullOrWhiteSpace(originalPath))
            {
                originalPath =
                    opportunity.Record.File.CurrentFullPath;
            }

            if (string.IsNullOrWhiteSpace(originalPath))
                return null;

            //---------------------------------------------------------
            // Retrieve the approved repair plan.
            //---------------------------------------------------------

            E_RepairPlan? repairPlan =
                GetRepairPlan(originalPath);

            if (repairPlan == null)
                return null;

            if (repairPlan.Changes.Count == 0)
                return null;

            //---------------------------------------------------------
            // Create ONE working copy.
            //
            // Every approved repair will be applied to this same copy.
            //---------------------------------------------------------

            string workingPath =
                _repairWorkspace.CreateWorkingCopy(
                    opportunity.Record.File);

            //---------------------------------------------------------
            // Apply every approved repair in the plan.
            //
            // ISBN is the first supported physical repair type.
            //---------------------------------------------------------

            foreach (E_RepairChange change in repairPlan.Changes)
            {
                if (!change.CanExecute)
                    continue;

                bool repaired =
    _epubRepairResource.ApplyRepairChange(
        opportunity.Record.File,
        change,
        workingPath);

                if (!repaired)
                    return null;

            }

            //---------------------------------------------------------
            // The repaired working copy is now the current version
            // of this EPUB.
            //
            // OriginalFullPath remains unchanged as the stable
            // identity of the source ebook.
            //
            // CurrentFullPath moves forward to the repaired copy so
            // the next Ebook Expert investigation reads the repaired
            // EPUB rather than starting over from the original.
            //---------------------------------------------------------

            opportunity.Record.File.CurrentFullPath =
                workingPath;

            opportunity.Record.File.CurrentName =
                Path.GetFileName(workingPath);

            //---------------------------------------------------------
            // Preserve the completed repaired EPUB for later
            // workflow/output handling.
            //---------------------------------------------------------

            _preparedFiles[originalPath] =
                workingPath;

            //---------------------------------------------------------
            // These approved changes have now been physically applied.
            // A later repair cycle must create a new plan containing
            // only newly approved changes.
            //---------------------------------------------------------

            repairPlan.Changes.Clear();

            return workingPath;
        }
    }
}