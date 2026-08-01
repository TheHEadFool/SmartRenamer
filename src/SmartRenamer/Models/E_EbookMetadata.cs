namespace SmartRenamer.Models
{
    /// <summary>
    /// =========================================================================
    /// E_EbookMetadata
    /// =========================================================================
    ///
    /// Represents metadata extracted from a single EPUB file.
    ///
    /// Future builds will gradually add:
    ///
    /// • Author
    /// • Publisher
    /// • Language
    /// • ISBN
    /// • Series
    /// • Description
    /// • Cover
    ///
    /// =========================================================================
    /// </summary>
    public sealed class E_EbookMetadata
    {
        public string Title { get; set; } = "";
    }
}