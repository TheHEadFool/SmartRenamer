using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.Specialists
{
    /// <summary>
    /// =========================================================================
    /// E_EbookMetadataSpecialist
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Understand the identity of every book before organizing the library."
    ///
    /// Domain
    /// -------------------------------------------------------------------------
    /// eBook Metadata
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Identifies EPUB files that will eventually be analyzed for metadata.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// This Specialist is the foundation of Scout's ebook understanding.
    /// Future builds will read metadata, covers, ISBNs, series information,
    /// descriptions and other information stored inside ebook files.
    ///
    /// Responsibilities (Current Build)
    /// -------------------------------------------------------------------------
    /// • Recognize EPUB files
    /// • Count EPUB files
    /// • Report whether any EPUBs were found
    ///
    /// Future Responsibilities
    /// -------------------------------------------------------------------------
    /// • Read metadata
    /// • Report missing metadata
    /// • Emit ObservationSignals
    /// • Ask unanswered questions
    /// =========================================================================
    /// </summary>
    public sealed class E_EbookMetadataSpecialist : ObservationSpecialist
    {
        public override string Name => "eBook Metadata Analysis";

        public override string Summary =>
            "Identifies EPUB files for future metadata analysis.";

        public override ExpertFinding Observe(
            IReadOnlyList<FileContext> files)
        {
            int epubCount =
                files.Count(IsEpubFile);

            if (epubCount == 0)
            {
                return new ExpertFinding
                {
                    FoundSomething = false,
                    Summary = "No EPUB files found."
                };
            }

            var finding = new ExpertFinding
            {
                FoundSomething = true,
                Summary = $"Found {epubCount} EPUB file(s)."
            };

            finding.Evidence.Add(
                $"EPUB Files: {epubCount}");

            finding.Questions.Add(
                "Ready to read EPUB metadata.");

            return finding;
        }

        private static bool IsEpubFile(
            FileContext file)
        {
            return file.Extension.Equals(
                ".epub",
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}