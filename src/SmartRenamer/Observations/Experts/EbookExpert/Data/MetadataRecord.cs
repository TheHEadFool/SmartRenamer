using SmartRenamer.Models;

namespace Scout.Observations.Experts.EbookExpert.Data
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