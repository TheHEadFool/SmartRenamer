using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SmartRenamer.Services.Naming
{
    public class FilenameAnalysisEngine
    {
        public FilenameAnalysisResult Analyze(string filename)
        {
            FilenameAnalysisResult result = new();

            result.OriginalName = filename;

            result.Extension = Path.GetExtension(filename);
            result.HasExtension = !string.IsNullOrWhiteSpace(result.Extension);

            string name = Path.GetFileNameWithoutExtension(filename);

            //--------------------------------------------------
            // Capitalization
            //--------------------------------------------------

            result.IsAllUppercase =
                name == name.ToUpperInvariant();

            result.IsAllLowercase =
                name == name.ToLowerInvariant();

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

            //--------------------------------------------------
            // Spacing
            //--------------------------------------------------

            result.ContainsUnderscores =
                name.Contains('_');

            if (result.ContainsUnderscores)
                result.Observations.Add("Filename contains underscores.");

            result.ContainsExtraSpaces =
                name.Contains("  ");

            if (result.ContainsExtraSpaces)
                result.Observations.Add("Filename contains extra spaces.");

            //--------------------------------------------------
            // Common generated names
            //--------------------------------------------------

            result.LooksLikeCameraFile =
                Regex.IsMatch(
                    name,
                    @"^(IMG|DSC|PXL|MVIMG)[-_ ]?\d+$",
                    RegexOptions.IgnoreCase);

            if (result.LooksLikeCameraFile)
                result.Observations.Add("Looks like a camera-generated filename.");

            result.LooksLikeTrackNumber =
                Regex.IsMatch(
                    name,
                    @"^(Track|Song)?[-_ ]?\d+$",
                    RegexOptions.IgnoreCase);

            if (result.LooksLikeTrackNumber)
                result.Observations.Add("Looks like a generic track number.");

            //--------------------------------------------------
            // Suggestions
            //--------------------------------------------------

            if (result.IsAllLowercase)
            {
                result.Suggestions.Add(new NamingSuggestion
                {
                    ProposedName = ToTitleCase(name) + result.Extension,
                    Reason = "Convert to Title Case.",
                    Confidence = SuggestionConfidence.High
                });
            }

            if (result.ContainsUnderscores)
            {
                result.Suggestions.Add(new NamingSuggestion
                {
                    ProposedName = name.Replace('_', ' ') + result.Extension,
                    Reason = "Replace underscores with spaces.",
                    Confidence = SuggestionConfidence.High
                });
            }

            return result;
        }

        private static string ToTitleCase(string value)
        {
            string[] words = value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0)
                    continue;

                words[i] =
                    char.ToUpper(words[i][0]) +
                    words[i].Substring(1).ToLower();
            }

            return string.Join(" ", words);
        }
    }
}