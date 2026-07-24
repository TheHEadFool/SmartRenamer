namespace SmartRenamer.Observations
{
    using System.Collections.Generic;
    using SmartRenamer.Observations.Specialists;
    /// <summary>
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains compressed archives.
    ///
    /// The expert does not organize files directly. Its responsibility is to
    /// recognize patterns, explain why they matter, and help Scout understand
    /// the collection so Scout can recommend safe organization opportunities.
    /// </summary>
    public class ArchiveExpert : ObservationExpert
    {
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
[
];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;
        public override string Name => "Archive Collection";

        public override string Summary =>
            "I noticed compressed archive files.";

        public override string WhyItMatters =>
            "Archives often contain projects, backups, or installers that benefit from being grouped separately from everyday files.";
    }
}