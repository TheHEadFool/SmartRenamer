using SmartRenamer.Observations.Specialists;
using System.Collections.Generic;

namespace SmartRenamer.Observations
{
    using System.Collections.Generic;
    using SmartRenamer.Observations.Specialists;
    /// <summary>
    /// The DocumentExpert helps Scout recognize document collections.
    ///
    /// It identifies files such as PDF documents, Word documents,
    /// spreadsheets, presentations, text files, and other information
    /// intended to be read or referenced.
    ///
    /// The expert does not organize files directly. Its responsibility is
    /// to help Scout understand what it has discovered so Scout can offer
    /// meaningful organization opportunities to the user.
    /// </summary>
    public class DocumentExpert : ObservationExpert
    {
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
[
];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;
        public override string Name => "Document Collection";

        public override string Summary =>
            "I noticed documents that appear to belong together.";

        public override string WhyItMatters =>
            "Keeping related documents together makes information easier to find, manage, and preserve.";
    }
}