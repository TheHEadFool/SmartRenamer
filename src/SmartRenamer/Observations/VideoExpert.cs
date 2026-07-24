using SmartRenamer.Observations.Specialists;
using System.Collections.Generic;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains a meaningful collection of videos.
    ///
    /// The expert does not organize files directly. Its responsibility is to
    /// recognize patterns, explain why they matter, and help Scout understand
    /// the collection so Scout can recommend safe organization opportunities.
    /// </summary>
    public class VideoExpert : ObservationExpert
    {
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
[
];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;
        public override string Name => "Video Collection";

        public override string Summary =>
            "I noticed videos that appear to belong together.";

        public override string WhyItMatters =>
            "Keeping related videos together makes them easier to browse, archive, and enjoy.";
    }
}