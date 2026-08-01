using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Models;
using SmartRenamer.Observations.BuildingBlocks;

namespace SmartRenamer.Observations.Specialists
{
    /// <summary>
    /// =========================================================================
    /// E_EbookMetadataSpecialist
    /// =========================================================================
    /// Observes EPUB metadata and reports the overall health of an ebook
    /// collection.
    /// =========================================================================
    /// </summary>
    public sealed class E_EbookMetadataSpecialist : ObservationSpecialist
    {
        public override string Name => "eBook Metadata";

        public override string Summary =>
            "Analyzes metadata stored inside EPUB files.";

        public override ExpertFinding Observe(
            IReadOnlyList<FileContext> files)
        {
            int epubFiles = 0;
            int titles = 0;
            int authors = 0;
            int isbns = 0;
            int covers = 0;

            foreach (FileContext file in files.Where(IsEpub))
            {
                epubFiles++;

                E_EbookMetadata? metadata =
                    E_EbookMetadataReader.Read(file);

                if (metadata == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(metadata.Title))
                    titles++;

                if (!string.IsNullOrWhiteSpace(metadata.Author))
                    authors++;

                if (!string.IsNullOrWhiteSpace(metadata.Isbn))
                    isbns++;

                if (metadata.HasCover)
                    covers++;
            }

            if (epubFiles == 0)
            {
                return new ExpertFinding
                {
                    FoundSomething = false,
                    Summary = "No EPUB files found."
                };
            }

            ExpertFinding finding = new()
            {
                FoundSomething = true,
                Summary = $"Analyzed {epubFiles} EPUB files."
            };

            finding.Evidence.Add($"Titles: {titles}/{epubFiles}");
            finding.Evidence.Add($"Authors: {authors}/{epubFiles}");
            finding.Evidence.Add($"ISBNs: {isbns}/{epubFiles}");
            finding.Evidence.Add($"Covers: {covers}/{epubFiles}");

            return finding;
        }

        private static bool IsEpub(
            FileContext file)
        {
            return file.Extension.Equals(
                ".epub",
                System.StringComparison.OrdinalIgnoreCase);
        }
    }
}