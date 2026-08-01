namespace SmartRenamer.Models
{
    /// <summary>
    /// =========================================================================
    /// E_EbookMetadata
    /// =========================================================================
    /// Represents metadata extracted from a single EPUB.
    /// =========================================================================
    /// </summary>
    public sealed class E_EbookMetadata
    {
        public string Title { get; set; } = "";

        public string Author { get; set; } = "";
    }
}