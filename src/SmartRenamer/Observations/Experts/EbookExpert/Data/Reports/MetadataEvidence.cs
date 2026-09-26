using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Data.Reports
{
    /// <summary>
    /// Preserves evidence discovered while analyzing ebook metadata.
    ///
    /// Evidence is deliberately separate from observed metadata so Scout can
    /// compare sources without silently replacing the value read from the EPUB.
    /// </summary>
    public class MetadataEvidence
    {
        /// <summary>
        /// Broad source of the evidence, for example EPUB Metadata, Filename,
        /// Opening Content, External Research, or User.
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>
        /// Metadata field to which the evidence relates.
        /// </summary>
        public string Field { get; set; } = "";

        /// <summary>
        /// The value actually observed or supplied.
        /// </summary>
        public string Value { get; set; } = "";

        /// <summary>
        /// Where the evidence was found within its source, when known.
        /// </summary>
        public string Location { get; set; } = "";

        /// <summary>
        /// Additional neutral context about the evidence.
        /// </summary>
        public string Notes { get; set; } = "";

        /// <summary>
        /// Existing compatibility collection used by duplicate-evidence
        /// reporting. It remains available while the richer evidence model
        /// is introduced.
        /// </summary>
        public List<string> Files { get; } = new();

        /// <summary>
        /// Legacy category retained for existing consumers. New evidence
        /// should prefer Source + Field.
        /// </summary>
        public string Category
        {
            get => string.IsNullOrWhiteSpace(Field)
                ? Source
                : $"{Source}: {Field}";
            set
            {
                if (string.IsNullOrWhiteSpace(Source) &&
                    !string.IsNullOrWhiteSpace(value))
                {
                    Source = value;
                }
            }
        }
    }
}
