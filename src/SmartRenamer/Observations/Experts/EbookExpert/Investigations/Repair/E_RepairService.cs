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
        /// Creates a temporary working copy and applies an explicitly
        /// approved ISBN to that copy.
        ///
        /// The original ebook is never modified.
        /// </summary>
        public bool PrepareIsbnRepair(
            RepairOpportunity opportunity,
            string approvedIsbn)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (string.IsNullOrWhiteSpace(approvedIsbn))
                throw new ArgumentException(
                    "Approved ISBN cannot be empty.",
                    nameof(approvedIsbn));

            if (!opportunity.MissingIsbn)
                return false;

            if (opportunity.Record?.File == null)
                return false;

            //---------------------------------------------------------
            // Create an isolated working copy.
            //---------------------------------------------------------

            string workingPath =
                _repairWorkspace.CreateWorkingCopy(
                    opportunity.Record.File);

            //---------------------------------------------------------
            // Apply the approved ISBN to the working copy.
            //---------------------------------------------------------

            bool repaired =
                _epubRepairResource.AddIsbn(
                    opportunity.Record.File,
                    approvedIsbn,
                    workingPath);

            if (!repaired)
                return false;

            //---------------------------------------------------------
            // Verify the actual working copy.
            //---------------------------------------------------------

            bool verified =
                VerifyIsbn(
                    opportunity,
                    approvedIsbn,
                    workingPath);

            if (!verified)
                return false;

            //---------------------------------------------------------
            // Preserve the prepared copy for the later Scout
            // organization handoff.
            //---------------------------------------------------------

            string originalPath =
                opportunity.Record.File.OriginalFullPath;

            if (string.IsNullOrWhiteSpace(originalPath))
            {
                originalPath =
                    opportunity.Record.File.CurrentFullPath;
            }

            if (string.IsNullOrWhiteSpace(originalPath))
                return false;

            _preparedFiles[originalPath] =
                workingPath;

            return true;
        }

        /// <summary>
        /// Returns the prepared working copy associated with an original ebook.
        ///
        /// Returns null when no repaired working copy has been prepared.
        /// </summary>
        public string? GetPreparedFile(
            string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                return null;

            return _preparedFiles.TryGetValue(
                originalPath,
                out string? preparedPath)
                ? preparedPath
                : null;
        }

        /// <summary>
        /// Applies an explicitly approved ISBN to a supplied target path.
        ///
        /// This method is retained as the low-level service operation.
        /// </summary>
        public bool ApplyIsbn(
            RepairOpportunity opportunity,
            string isbn,
            string targetPath)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException(
                    "ISBN cannot be empty.",
                    nameof(isbn));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(
                    "Target path cannot be empty.",
                    nameof(targetPath));

            //---------------------------------------------------------
            // Do not repair an ebook that the Repair Block did not
            // identify as missing an ISBN.
            //---------------------------------------------------------

            if (!opportunity.MissingIsbn)
                return false;

            //---------------------------------------------------------
            // The RepairOpportunity contains the FileContext used
            // for ebook metadata and extension information.
            //---------------------------------------------------------

            if (opportunity.Record?.File == null)
                return false;

            //---------------------------------------------------------
            // Delegate the physical EPUB modification to the Resource.
            //---------------------------------------------------------

            return _epubRepairResource.AddIsbn(
                opportunity.Record.File,
                isbn,
                targetPath);
        }

        /// <summary>
        /// Verifies that the supplied ISBN is present in the specified
        /// EPUB path.
        ///
        /// The EPUB is re-read using the shared Ebook metadata reader.
        /// This method does not modify the ebook.
        /// </summary>
        public bool VerifyIsbn(
            RepairOpportunity opportunity,
            string isbn,
            string targetPath)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException(
                    "ISBN cannot be empty.",
                    nameof(isbn));

            if (string.IsNullOrWhiteSpace(targetPath))
                throw new ArgumentException(
                    "Target path cannot be empty.",
                    nameof(targetPath));

            if (opportunity.Record?.File == null)
                return false;

            if (!File.Exists(targetPath))
                return false;

            //---------------------------------------------------------
            // Build a temporary FileContext representing the prepared
            // EPUB. The original FileContext remains unchanged.
            //---------------------------------------------------------

            FileContext workingFile = new()
            {
                OriginalFullPath =
                    opportunity.Record.File.OriginalFullPath,

                OriginalName =
                    opportunity.Record.File.OriginalName,

                CurrentFullPath =
                    targetPath,

                CurrentName =
                    Path.GetFileName(targetPath),

                Extension =
                    Path.GetExtension(targetPath),

                DestinationFolder =
                    opportunity.Record.File.DestinationFolder,

                DestinationName =
                    opportunity.Record.File.DestinationName
            };

            //---------------------------------------------------------
            // Re-read the prepared EPUB.
            //---------------------------------------------------------

            E_EbookMetadata? metadata =
                E_EbookMetadataReader.Read(
                    workingFile);

            if (metadata == null)
                return false;

            //---------------------------------------------------------
            // Verification succeeds only when the EPUB reports
            // the exact ISBN supplied for the repair.
            //---------------------------------------------------------

            return string.Equals(
                metadata.Isbn?.Trim(),
                isbn.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Performs a complete approved ISBN repair in a temporary
        /// Ebook Expert working copy.
        ///
        /// The original ebook is never modified.
        /// </summary>
        public bool RepairIsbn(
            RepairOpportunity opportunity,
            string approvedIsbn)
        {
            return PrepareIsbnRepair(
                opportunity,
                approvedIsbn);
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

    }
}