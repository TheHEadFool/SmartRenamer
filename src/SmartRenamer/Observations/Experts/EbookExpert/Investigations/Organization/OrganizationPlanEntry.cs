namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Represents the planned organization destination for one ebook.
    ///
    /// OriginalPath is the stable identity of the source ebook.
    ///
    /// RelativeDestinationFolder describes the destination beneath
    /// OrganizationOptions.DestinationRoot.
    ///
    /// DestinationFileName contains the filename Scout intends to use
    /// for the organized copy.
    ///
    /// This class performs no filesystem operations.
    /// </summary>
    public sealed class OrganizationPlanEntry
    {
        /// <summary>
        /// Stable identity of the source ebook.
        /// </summary>
        public string OriginalPath { get; set; } = "";

        /// <summary>
        /// Folder path relative to OrganizationOptions.DestinationRoot.
        /// </summary>
        public string RelativeDestinationFolder { get; set; } = "";

        /// <summary>
        /// Filename to use for the organized ebook.
        /// </summary>
        public string DestinationFileName { get; set; } = "";

        /// <summary>
        /// Indicates whether this entry has a source identity and
        /// a planned destination filename.
        /// </summary>
        public bool IsPlanned =>
            !string.IsNullOrWhiteSpace(OriginalPath) &&
            !string.IsNullOrWhiteSpace(DestinationFileName);
    }
}