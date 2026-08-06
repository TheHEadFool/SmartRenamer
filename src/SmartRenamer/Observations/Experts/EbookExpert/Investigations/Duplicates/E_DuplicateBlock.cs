using System;
using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.BuildingBlocks;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Duplicates
{
    /// <summary>
    /// =========================================================================
    /// E_DuplicateBlock
    /// =========================================================================
    ///
    /// Collects factual information describing duplicate relationships
    /// within an ebook library.
    ///
    /// Blocks discover facts.
    /// Blocks never make recommendations.
    /// =========================================================================
    /// </summary>
    internal sealed class E_DuplicateBlock
    {
        public E_DuplicateReport Analyze(
            IReadOnlyList<FileContext> files)
        {
            E_DuplicateReport report = new();
            Dictionary<string, int> titleCounts = new();
            Dictionary<string, int> isbnCounts = new();

            //-----------------------------------------------------
            // Read every EPUB in the collection.
            //-----------------------------------------------------

            foreach (FileContext file in files)
            {
                if (!file.Extension.Equals(
                    ".epub",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                E_EbookMetadata? metadata =
                    E_EbookMetadataReader.Read(file);

                if (metadata == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(metadata.Title))
                {
                    if (!titleCounts.ContainsKey(metadata.Title))
                    {
                        titleCounts[metadata.Title] = 0;
                    }

                    titleCounts[metadata.Title]++;
                }
                if (!string.IsNullOrWhiteSpace(metadata.Isbn))
                {
                    if (!isbnCounts.ContainsKey(metadata.Isbn))
                    {
                        isbnCounts[metadata.Isbn] = 0;
                    }

                    isbnCounts[metadata.Isbn]++;
                }


            }

            foreach (KeyValuePair<string, int> title in titleCounts)
            {
                if (title.Value > 1)
                {
                    report.DuplicateTitles++;
                    report.DuplicateTitleList.Add(title.Key);

                }
            }
            foreach (KeyValuePair<string, int> isbn in isbnCounts)
            {
                if (isbn.Value > 1)
                {
                    report.DuplicateIsbns++;
                    report.DuplicateIsbnList.Add(isbn.Key);

                }
            }
            return report;
        }  //End Analyer()
    } // End E_DuplicateBlock
} // End namespace