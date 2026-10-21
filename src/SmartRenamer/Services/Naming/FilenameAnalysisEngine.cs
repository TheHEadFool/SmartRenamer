using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SmartRenamer.Services.Naming
{
    /// <summary>
    /// Analyzes and proposes safe, high-confidence filename cleanup.
    ///
    /// The first priority is the application's original purpose:
    /// remove underscores and obvious source-site prefixes while preserving
    /// the meaningful title, author, and file extension.
    /// This class proposes names only; it does not rename or move files.
    /// </summary>
    public class FilenameAnalysisEngine
    {
        public FilenameAnalysisResult Analyze(string filename)
        {
            FilenameAnalysisResult result = new();
            result.OriginalName = filename ?? string.Empty;

            result.Extension = Path.GetExtension(result.OriginalName);
            result.HasExtension = !string.IsNullOrWhiteSpace(result.Extension);

            string name = Path.GetFileNameWithoutExtension(result.OriginalName);
            string normalized = NormalizeFilenameStem(name);

            result.IsAllUppercase =
                name.Length > 0 && name == name.ToUpperInvariant();
            result.IsAllLowercase =
                name.Length > 0 && name == name.ToLowerInvariant();
            result.IsTitleCase =
                name.Length > 0 &&
                char.IsUpper(name[0]) &&
                !result.IsAllUppercase &&
                !result.IsAllLowercase;

            if (result.IsAllUppercase)
                result.Observations.Add("Filename is ALL CAPS.");
            if (result.IsAllLowercase)
                result.Observations.Add("Filename is all lowercase.");
            if (result.IsTitleCase)
                result.Observations.Add("Filename appears to use Title Case.");

            result.ContainsUnderscores = name.Contains('_');
            if (result.ContainsUnderscores)
                result.Observations.Add("Filename contains underscores.");

            result.ContainsExtraSpaces = Regex.IsMatch(name, @"\s{2,}");
            if (result.ContainsExtraSpaces)
                result.Observations.Add("Filename contains extra spaces.");

            result.LooksLikeCameraFile =
                Regex.IsMatch(name, @"^(IMG|DSC|PXL|MVIMG)[-_ ]?\d+$",
                    RegexOptions.IgnoreCase);
            if (result.LooksLikeCameraFile)
                result.Observations.Add("Looks like a camera-generated filename.");

            result.LooksLikeTrackNumber =
                Regex.IsMatch(name, @"^(Track|Song)?[-_ ]?\d+$",
                    RegexOptions.IgnoreCase);
            if (result.LooksLikeTrackNumber)
                result.Observations.Add("Looks like a generic track number.");

            // Produce one combined suggestion. Previously the planner selected
            // only the first high-confidence suggestion, so underscore cleanup
            // could prevent other obvious cleanup from being applied together.
            if (!string.Equals(normalized, name, StringComparison.Ordinal))
            {
                result.Suggestions.Add(new NamingSuggestion
                {
                    ProposedName = normalized + result.Extension,
                    Reason = BuildReason(name, normalized),
                    Confidence = SuggestionConfidence.High
                });
            }

            return result;
        }

        private static string NormalizeFilenameStem(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value ?? string.Empty;

            string cleaned = value.Trim();

            // Remove a leading underscore left by common download naming.
            cleaned = Regex.Replace(cleaned, @"^\s*_+", "");

            // Remove a website/domain prefix only at the start of the filename.
            // Keep the remainder intact; never remove arbitrary words from a title.
            cleaned = Regex.Replace(
                cleaned,
                @"^(?:www\.)?[a-z0-9][a-z0-9.-]*\.(?:com|net|org|edu|gov|io|co|info|biz|me|tv)\s*[_ -]+\s*",
                "",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            // Underscores and repeated whitespace are formatting, not metadata.
            cleaned = cleaned.Replace('_', ' ');
            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            // Normalize separators surrounded by whitespace without deleting
            // meaningful punctuation inside a title.
            cleaned = Regex.Replace(cleaned, @"\s*-\s*", " - ");
            cleaned = Regex.Replace(cleaned, @"(?:\s-\s){2,}", " - ");

            // Collapse a duplicated trailing author segment, e.g.
            // "Christmas Eve - by Jim Butcher - Jim Butcher".
            Match repeatedAuthor = Regex.Match(
                cleaned,
                @"^(?<title>.+?)\s-\sby\s+(?<author>.+?)\s-\s(?<repeat>.+?)$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (repeatedAuthor.Success &&
                string.Equals(
                    repeatedAuthor.Groups["author"].Value.Trim(),
                    repeatedAuthor.Groups["repeat"].Value.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                cleaned = repeatedAuthor.Groups["title"].Value.Trim()
                    + " - "
                    + repeatedAuthor.Groups["author"].Value.Trim();
            }
            else
            {
                // Also collapse an exact duplicate final segment without
                // assuming it is an author unless the two segments match.
                Match duplicateTail = Regex.Match(
                    cleaned,
                    @"^(?<prefix>.+?)\s-\s(?<last>[^-]+?)\s-\s(?<repeat>[^-]+?)$",
                    RegexOptions.CultureInvariant);

                if (duplicateTail.Success &&
                    string.Equals(
                        duplicateTail.Groups["last"].Value.Trim(),
                        duplicateTail.Groups["repeat"].Value.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = duplicateTail.Groups["prefix"].Value.Trim()
                        + " - "
                        + duplicateTail.Groups["last"].Value.Trim();
                }
            }

            return cleaned.Trim().Trim('-', ' ');
        }

        private static string BuildReason(string original, string normalized)
        {
            string reason = "Clean filename formatting";
            if (original.Contains('_'))
                reason += ", replace underscores with spaces";
            if (Regex.IsMatch(
                    original,
                    @"^(?:\s*_)*(?:www\.)?[a-z0-9][a-z0-9.-]*\.(?:com|net|org|edu|gov|io|co|info|biz|me|tv)\s*[_ -]+",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                reason += ", remove source-site prefix";
            if (normalized.Length < original.Length)
                reason += ", remove redundant filename text";
            return reason + ".";
        }
    }
}
