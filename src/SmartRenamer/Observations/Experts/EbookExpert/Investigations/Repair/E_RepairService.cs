using System;
using System.Collections.Generic;
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
    /// • Apply an approved ISBN.
    /// • Verify an ISBN after repair.
    /// • Perform a complete ISBN repair and verify the result.
    ///
    /// Safety Boundary
    /// -------------------------------------------------------------------------
    /// Research never modifies an ebook.
    ///
    /// Repair only occurs when an ISBN is explicitly supplied to RepairIsbn().
    ///
    /// A repair is considered successful only when the EPUB is re-read and
    /// the requested ISBN is confirmed in its metadata.
    ///
    /// This Service does NOT
    /// -------------------------------------------------------------------------
    /// • Decide whether an ebook needs repair.
    /// • Decide which recommendation Scout should present.
    /// • Interpret user conversation.
    /// • Select an ISBN automatically.
    /// • Automatically approve a repair.
    ///
    /// Those responsibilities belong to the Repair Investigation,
    /// Conversation Framework, and user.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_RepairService
    {
        private readonly E_IsbnResearchResource _isbnResearchResource = new();

        private readonly E_EpubRepairResource _epubRepairResource = new();

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
        /// Applies an explicitly approved ISBN to the ebook represented
        /// by the RepairOpportunity.
        ///
        /// The ISBN is never selected by this method.
        /// The caller must provide the ISBN that has already been approved.
        /// </summary>
        public bool ApplyIsbn(
            RepairOpportunity opportunity,
            string isbn)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException(
                    "ISBN cannot be empty.",
                    nameof(isbn));

            //---------------------------------------------------------
            // Do not repair an ebook that the Repair Block did not
            // identify as missing an ISBN.
            //---------------------------------------------------------

            if (!opportunity.MissingIsbn)
                return false;

            //---------------------------------------------------------
            // The RepairOpportunity contains the original FileContext.
            //---------------------------------------------------------

            if (opportunity.Record?.File == null)
                return false;

            //---------------------------------------------------------
            // Delegate the physical EPUB modification to the Resource.
            //---------------------------------------------------------

            return _epubRepairResource.AddIsbn(
                opportunity.Record.File,
                isbn);
        }

        /// <summary>
        /// Verifies that the supplied ISBN is now present in the EPUB.
        ///
        /// The EPUB is re-read using the shared Ebook metadata reader.
        /// This method does not modify the ebook.
        /// </summary>
        public bool VerifyIsbn(
            RepairOpportunity opportunity,
            string isbn)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (string.IsNullOrWhiteSpace(isbn))
                throw new ArgumentException(
                    "ISBN cannot be empty.",
                    nameof(isbn));

            //---------------------------------------------------------
            // The RepairOpportunity contains the FileContext required
            // by the shared metadata reader.
            //---------------------------------------------------------

            if (opportunity.Record?.File == null)
                return false;

            //---------------------------------------------------------
            // Re-read the EPUB after repair.
            //---------------------------------------------------------

            E_EbookMetadata? metadata =
                E_EbookMetadataReader.Read(
                    opportunity.Record.File);

            if (metadata == null)
                return false;

            //---------------------------------------------------------
            // Verification succeeds only when the EPUB now reports
            // the exact ISBN that was supplied for the repair.
            //---------------------------------------------------------

            return string.Equals(
                metadata.Isbn?.Trim(),
                isbn.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Performs a complete approved ISBN repair.
        ///
        /// The operation:
        ///
        ///     1. Applies the supplied ISBN.
        ///     2. Re-reads the EPUB.
        ///     3. Verifies that the ISBN is present.
        ///
        /// No ISBN is selected or invented by this method.
        /// The caller must supply the approved ISBN.
        /// </summary>
        public bool RepairIsbn(
            RepairOpportunity opportunity,
            string approvedIsbn)
        {
            if (opportunity == null)
                throw new ArgumentNullException(nameof(opportunity));

            if (string.IsNullOrWhiteSpace(approvedIsbn))
                throw new ArgumentException(
                    "Approved ISBN cannot be empty.",
                    nameof(approvedIsbn));

            //---------------------------------------------------------
            // Perform the physical repair.
            //---------------------------------------------------------

            bool repaired =
                ApplyIsbn(
                    opportunity,
                    approvedIsbn);

            if (!repaired)
                return false;

            //---------------------------------------------------------
            // Never report success until the result has been verified.
            //---------------------------------------------------------

            return VerifyIsbn(
                opportunity,
                approvedIsbn);
        }
    }
}