using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    /// <summary>
    /// Interprets explicit bibliographic fields found in the EPUB's already-extracted
    /// opening documents. This consultant never edits EPUB metadata. It publishes
    /// sourced evidence into the same MetadataReport/MetadataRecord evidence pool
    /// consumed by existing repair evaluators.
    /// </summary>
    internal sealed class E_FrontMatterConsultant
    {
        private const string SourceName = "Front Matter Consultant";

        // Supports both standalone labels and inline cataloging fields, e.g.
        // "Description: New York : Ace, [2023] | Series: The Cinder Spires".
        // Values stop at a field delimiter or a recognized following catalog field.
        private static readonly Regex SeriesField = new(
            @"(?im)(?:^|[|;\r\n])\s*Series(?:\s+Title)?\s*:\s*(?<value>.*?)(?=\s*(?:[|;\r\n]|\b(?:Identifiers|Subjects|Notes|Edition|Other\s+Titles|Classification|Description|Publisher|Publication\s+Date|ISBN|Language)\s*:|$))",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex SeriesNumberField = new(
            @"(?im)(?:^|[|;\r\n])\s*(?:Series\s+Number|Series\s+Position|Position\s+in\s+Series)\s*:\s*(?<value>\d+(?:\.\d+)?)\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Library of Congress cataloging statements often encode a series in the
        // title/creator paragraph rather than in a literal "Series:" field, e.g.
        // "... The aeronaut's windlass/Jim Butcher. p. cm.—(Cinder spires) ISBN ...".
        // Restrict this fallback to the recognizable LC cataloging context and the
        // catalog's p. cm. delimiter; do not treat arbitrary parentheses as series.
        private static readonly Regex CatalogSeriesStatement = new(
            @"(?is)LIBRARY\s+OF\s+CONGRESS\s+CATALOGING-IN-PUBLICATION\s+DATA\s*:.*?\bp\.\s*cm\.?\s*[—–-]\s*\((?<value>[^)\r\n]{3,100})\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public int Contribute(MetadataReport report, MetadataRecord record,
            string content, string documentPath)
        {
            ArgumentNullException.ThrowIfNull(report);
            ArgumentNullException.ThrowIfNull(record);
            if (string.IsNullOrWhiteSpace(content)) return 0;

            int added = 0;
            List<string> seriesValues = new();
            foreach (Match match in SeriesField.Matches(content))
            {
                string value = CleanValue(match.Groups["value"].Value);
                if (!IsPlausibleSeries(value)) continue;
                seriesValues.Add(value);
                added += Add(report, record, "Series", value, documentPath,
                    "Explicit Series field parsed from opening-document bibliographic text.");
            }

            // LC catalog records commonly use a parenthetical series statement
            // after "p. cm." instead of a labeled Series field. This contributes
            // series identity only; it never infers a series number or position.
            foreach (Match match in CatalogSeriesStatement.Matches(content))
            {
                string value = CleanValue(match.Groups["value"].Value);
                if (!IsPlausibleSeries(value)) continue;
                seriesValues.Add(value);
                added += Add(report, record, "Series", value, documentPath,
                    "Series label parsed from a Library of Congress-style title/creator catalog statement. No series number inferred.");
            }

            // A position is meaningful only when this same document explicitly
            // identifies the series. Never attach an isolated number to a book.
            // A position can only be associated safely when this document yields
            // one unambiguous series label. If the same front matter names multiple
            // series, retain the series evidence but do not attach a number to all of them.
            if (seriesValues.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
            {
                foreach (Match match in SeriesNumberField.Matches(content))
                {
                    string value = match.Groups["value"].Value.Trim();
                    if (!Regex.IsMatch(value, @"^\d+(?:\.\d+)?$")) continue;
                    added += Add(report, record, "SeriesNumber", value, documentPath,
                        "Explicit series-position field parsed alongside an explicit Series field in the same opening document.");
                }
            }

            // ISBN evidence is already extracted by E_MetadataBlock with checksum
            // validation. Do not duplicate that existing path here.
            return added;
        }

        private static int Add(MetadataReport report, MetadataRecord record,
            string field, string value, string location, string notes)
        {
            if (record.Evidence.Any(e =>
                string.Equals(e.Source, SourceName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Field, field, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Value, value, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(e.Location, location, StringComparison.OrdinalIgnoreCase))) return 0;

            report.AddEvidence(record, new MetadataEvidence
            {
                Source = SourceName,
                Field = field,
                Value = value,
                Location = location ?? string.Empty,
                Notes = notes
            });
            return 1;
        }

        private static string CleanValue(string value)
        {
            string clean = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
            return clean.Trim(' ', '\t', '.', ':', ';', '-', '–', '—', '"', '\'');
        }

        private static bool IsPlausibleSeries(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 3 || value.Length > 100) return false;
            if (Regex.IsMatch(value, @"^(?:cover|jacket|interior|copyright|title page|table of contents|publisher|isbn|by|none|n/?a)\b", RegexOptions.IgnoreCase)) return false;
            return value.Any(char.IsLetter);
        }
    }
}
