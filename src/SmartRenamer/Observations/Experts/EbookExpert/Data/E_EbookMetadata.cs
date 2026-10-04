namespace Scout.Observations.Experts.EbookExpert.Data
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

        /// <summary>
        /// Position of this book within its series, when observed.
        /// The value remains text so decimal positions such as 4.5 are preserved.
        /// </summary>
        public string SeriesNumber { get; set; } = "";

        public string Description { get; set; } = "";

        public bool HasCover { get; set; }

        public byte[]? CoverImage { get; set; }
    }
}