using SmartRenamer.Observations;
using SmartRenamer.Observations.Specialists;
using System.Collections.Generic;

namespace SmartRenamer.Observations
{
    using System.Collections.Generic;
    using SmartRenamer.Observations.Specialists;
    /// <summary>
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains a meaningful collection of ebooks.
    ///
    /// The expert does not organize files directly. Its responsibility is to
    /// recognize patterns, explain why they matter, and help Scout understand
    /// the collection so Scout can recommend safe organization opportunities.
    /// </summary>
    public class EbookExpert : ObservationExpert
{
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
[
];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;
        public override string Name => "eBook Library";

    public override string Summary =>
        "I noticed what appears to be a collection of ebooks.";

    public override string WhyItMatters =>
        "Keeping ebooks organized by author, series, or subject makes your library easier to browse, search, and enjoy.";
}
}