using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.Specialists
{
    public class AlbumSpecialist : ObservationSpecialist
    {
        private static readonly HashSet<string> AudioExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3",
                ".flac",
                ".wav",
                ".aac",
                ".m4a",
                ".ogg",
                ".wma",
                ".opus",
                ".aiff",
                ".alac"
            };

        public override string Name => "Album Recognition";

        public override string Summary =>
            "Recognizes music that appears to belong to complete albums.";

        public override ExpertFinding Observe(
            IReadOnlyList<FileContext> files)
        {
            var musicFiles = files
                .Where(IsAudioFile)
                .ToList();

            if (musicFiles.Count == 0)
            {
                return new ExpertFinding
                {
                    FoundSomething = false,
                    Summary = string.Empty
                };
            }

            var folders = musicFiles
                .GroupBy(f => f.ParentFolder)
                .ToList();

            int albumCount = 0;

            var evidence = new List<string>();

            foreach (var folder in folders)
            {
                var folderFiles = folder.ToList();

                int trackCount = folderFiles.Count;

                bool sameExtension =
                    folderFiles
                        .Select(f => f.Extension)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count() == 1;

                int score = 0;

                if (trackCount >= 8)
                    score += 3;
                else if (trackCount >= 5)
                    score += 2;
                else if (trackCount >= 2)
                    score += 1;

                if (sameExtension)
                    score++;

                if (score >= 3)
                {
                    albumCount++;

                    evidence.Add(
                        $"{Path.GetFileName(folder.Key)} ({trackCount} tracks)");
                }
            }

            if (albumCount == 0)
            {
                return new ExpertFinding
                {
                    FoundSomething = false,
                    Summary = "No album folders detected."
                };
            }

            var finding = new ExpertFinding
            {
                FoundSomething = true,
                Summary = $"Detected {albumCount} album folder(s)."
            };

            foreach (var item in evidence)
            {
                finding.Evidence.Add(item);
            }

            finding.Questions.Add("Metadata has not yet been analyzed.");
            finding.Questions.Add("Artwork has not yet been analyzed.");
            finding.Questions.Add("Track numbering has not yet been verified.");

            return finding;
        }

        private static bool IsAudioFile(FileContext file)
        {
            return AudioExtensions.Contains(file.Extension);
        }
    }
}