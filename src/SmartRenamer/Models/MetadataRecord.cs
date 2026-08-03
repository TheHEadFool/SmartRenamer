using SmartRenamer.Models;

namespace SmartRenamer.Observations.Experts.EbookExpert.Data.Models
{
    /// <summary>
    /// Represents one researched ebook and the file it came from.
    /// </summary>
    public sealed class MetadataRecord
    {
        public FileContext File { get; init; } = null!;

        public E_EbookMetadata Metadata { get; init; } = null!;
    }
}