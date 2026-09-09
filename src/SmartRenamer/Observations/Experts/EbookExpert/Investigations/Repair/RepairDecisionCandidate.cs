namespace Scout.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Represents a possible value for an Ebook repair opportunity.
    ///
    /// This class is intentionally domain-neutral within the Ebook Expert.
    /// It can represent a candidate ISBN, title, author, publisher, language,
    /// description, cover, or another repairable value.
    ///
    /// The candidate carries evidence and confidence supplied by the
    /// domain-specific research that produced it. It does not decide whether
    /// the candidate should be used.
    /// </summary>
    public sealed class RepairDecisionCandidate
    {
        /// <summary>
        /// The proposed value for the repair opportunity.
        /// </summary>
        public object? Value { get; init; }

        /// <summary>
        /// The source from which the candidate value was obtained.
        /// </summary>
        public string Source { get; init; } = "";

        /// <summary>
        /// Evidence supporting the candidate value.
        /// </summary>
        public string Evidence { get; init; } = "";

        /// <summary>
        /// The confidence assigned by the domain-specific research process.
        /// </summary>
        public double Confidence { get; init; }

        /// <summary>
        /// Indicates that the domain-specific research has determined this
        /// candidate to be the preferred candidate among the available
        /// alternatives.
        ///
        /// False does not mean the candidate is wrong. It means that this
        /// candidate has not been established as the preferred choice.
        /// </summary>
        public bool IsPreferred { get; init; }
    }
}