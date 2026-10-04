using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Scout.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Recognizes ISBN values printed in EPUB opening content.
    ///
    /// This class only discovers and validates ISBN-10 and ISBN-13 values.
    /// It does not perform external research, assign repair confidence,
    /// select a candidate, modify the EPUB, or communicate with the UI.
    /// </summary>
    internal sealed class E_IsbnContentRecognizer
    {
        private static readonly Regex CandidatePattern =
            new(
                @"(?<![0-9Xx])(?:[0-9Xx](?:[\s\-–—.]?[0-9Xx]){9,12})(?![0-9Xx])",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Finds valid ISBN-10 and ISBN-13 values in the supplied EPUB text.
        /// Punctuation and whitespace commonly used to format an ISBN are
        /// removed before validation.
        /// </summary>
        /// <param name="openingContent">Text extracted from the EPUB.</param>
        /// <returns>
        /// Distinct normalized ISBN values in the order in which they first
        /// appear in the source text.
        /// </returns>
        public IReadOnlyList<string> Recognize(
            string? openingContent)
        {
            if (string.IsNullOrWhiteSpace(openingContent))
                return Array.Empty<string>();

            HashSet<string> seen =
                new(StringComparer.OrdinalIgnoreCase);

            List<string> recognized = new();

            foreach (Match match in CandidatePattern.Matches(openingContent))
            {
                string isbn = Normalize(match.Value);

                if (!IsValidIsbn(isbn))
                    continue;

                if (seen.Add(isbn))
                    recognized.Add(isbn);
            }

            return recognized;
        }

        /// <summary>
        /// Removes formatting characters while preserving the ISBN check
        /// character X when present.
        /// </summary>
        private static string Normalize(string value)
        {
            return new string(
                value
                    .Where(character =>
                        char.IsDigit(character) ||
                        character is 'X' or 'x')
                    .ToArray())
                .ToUpperInvariant();
        }

        /// <summary>
        /// Validates an ISBN-10 or ISBN-13 using its check digit.
        /// </summary>
        private static bool IsValidIsbn(string isbn)
        {
            if (isbn.Length == 13)
            {
                if (!isbn.StartsWith("978", StringComparison.Ordinal) &&
                    !isbn.StartsWith("979", StringComparison.Ordinal))
                {
                    return false;
                }

                int sum = 0;

                for (int i = 0; i < isbn.Length; i++)
                {
                    if (!char.IsDigit(isbn[i]))
                        return false;

                    int digit = isbn[i] - '0';

                    sum +=
                        i % 2 == 0
                            ? digit
                            : digit * 3;
                }

                return sum % 10 == 0;
            }

            if (isbn.Length == 10)
            {
                int sum = 0;

                for (int i = 0; i < isbn.Length; i++)
                {
                    char character = isbn[i];
                    int value;

                    if (char.IsDigit(character))
                    {
                        value = character - '0';
                    }
                    else if (i == 9 &&
                             (character == 'X' || character == 'x'))
                    {
                        value = 10;
                    }
                    else
                    {
                        return false;
                    }

                    sum += (10 - i) * value;
                }

                return sum % 11 == 0;
            }

            return false;
        }
    }
}
