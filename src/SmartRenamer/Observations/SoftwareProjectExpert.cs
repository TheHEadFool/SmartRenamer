using SmartRenamer.Observations.Specialists;
using System.Collections.Generic;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains a software project.
    ///
    /// The expert does not organize files directly. Its responsibility is to
    /// recognize patterns, explain why they matter, and help Scout understand
    /// the collection so Scout can recommend safe organization opportunities.
    /// </summary>
    public class SoftwareProjectExpert : ObservationExpert
    {
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
[
];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;
        public override string Name => "Software Project";

        public override string Summary =>
            "I noticed files that appear to belong to a software development project.";

        public override string WhyItMatters =>
            "Recognizing a software project helps preserve its structure and prevents important files from being separated.";
    }
}