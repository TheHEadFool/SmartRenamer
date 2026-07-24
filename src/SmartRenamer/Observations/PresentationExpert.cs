namespace SmartRenamer.Observations
{
    using System.Collections.Generic;
    using SmartRenamer.Observations.Specialists;
    /// <summary>
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains presentations.
    ///
    /// The expert does not organize files directly. Its responsibility is to
    /// recognize patterns, explain why they matter, and help Scout understand
    /// the collection so Scout can recommend safe organization opportunities.
    /// </summary>
    public class PresentationExpert : ObservationExpert
    {
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
[
];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;
        public override string Name => "Presentation Collection";

        public override string Summary =>
            "I noticed presentation files that appear to belong together.";

        public override string WhyItMatters =>
            "Keeping presentations together makes projects and meetings easier to prepare and revisit.";
    }
}