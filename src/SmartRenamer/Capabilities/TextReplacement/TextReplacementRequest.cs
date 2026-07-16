namespace SmartRenamer.Capabilities.TextReplacement
{
    /// <summary>
    /// Describes a text replacement operation.
    /// </summary>
    public class TextReplacementRequest
    {
        public string FindText { get; set; } = "";

        public string ReplaceWith { get; set; } = "";

        public bool IgnoreCase { get; set; }

        public bool IncludeExtension { get; set; }
    }
}