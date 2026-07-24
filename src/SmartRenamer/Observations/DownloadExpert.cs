namespace SmartRenamer.Observations
{
    using System.Collections.Generic;
    using SmartRenamer.Observations.Specialists;
    /// <summary>
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains a collection of downloaded files.
    ///
    /// The expert does not organize files directly. Its responsibility is to
    /// recognize patterns, explain why they matter, and help Scout understand
    /// the collection so Scout can recommend safe organization opportunities.
    /// </summary>
    public class DownloadExpert : ObservationExpert
    {
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
[
];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;
        public override string Name => "Downloads";

        public override string Summary =>
            "I noticed this folder appears to be collecting downloaded files.";

        public override string WhyItMatters =>
            "Download folders often accumulate many unrelated files that can be safely organized into more meaningful groups.";
    }
}