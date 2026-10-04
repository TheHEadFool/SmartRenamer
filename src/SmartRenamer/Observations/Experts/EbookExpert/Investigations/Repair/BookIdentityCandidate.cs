namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// A coherent identity candidate for one ebook.
    ///
    /// The candidate keeps related identity fields together so that Title,
    /// Author, Series, and SeriesNumber can be evaluated as one book identity
    /// instead of as unrelated metadata values.
    /// </summary>
    public sealed class BookIdentityCandidate
    {
        public string Title { get; init; } = "";

        public string Authors { get; init; } = "";

        public string Series { get; init; } = "";

        public string SeriesNumber { get; init; } = "";
    }
}
