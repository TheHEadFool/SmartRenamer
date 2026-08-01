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

        public string Publisher { get; set; } = "";

        public string Language { get; set; } = "";

        public string Isbn { get; set; } = "";

        public string Series { get; set; } = "";

        public string Description { get; set; } = "";

        public bool HasCover { get; set; }
    }
}