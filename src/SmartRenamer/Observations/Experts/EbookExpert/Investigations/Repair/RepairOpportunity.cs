using SmartRenamer.Observations.Experts.EbookExpert.Data.Models;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// RepairOpportunity
    /// =========================================================================
    ///
    /// Represents one ebook and the specific repair opportunities discovered
    /// for that ebook.
    ///
    /// This class contains facts only.
    /// It does not decide whether a repair should be performed.
    /// =========================================================================
    /// </summary>
    public sealed class RepairOpportunity
    {
        public MetadataRecord Record { get; init; } = null!;

        public bool MissingTitle { get; init; }

        public bool MissingAuthor { get; init; }

        public bool MissingIsbn { get; init; }

        public bool MissingPublisher { get; init; }

        public bool MissingLanguage { get; init; }

        public bool MissingDescription { get; init; }

        public bool MissingCover { get; init; }
    }
}