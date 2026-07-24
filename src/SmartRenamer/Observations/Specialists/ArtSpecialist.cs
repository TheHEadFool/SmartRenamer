using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.Specialists
{
    /// <summary>
    /// -------------------------------------------------------------------------
    /// Domain
    /// -------------------------------------------------------------------------
    /// Music Artwork
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// This Specialist understands artwork associated with music collections.
    /// It reports factual observations about artwork so MusicExpert can combine
    /// them with findings from other Specialists.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// Album artwork is an important indicator of a complete, well-organized
    /// music library. This Specialist detects common artwork files without
    /// making recommendations.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Detect folder artwork
    /// • Report evidence
    /// • Ask follow-up questions
    ///
    /// This Specialist does NOT
    /// -------------------------------------------------------------------------
    /// • Recognize albums
    /// • Read music metadata
    /// • Analyze playlists
    /// • Recommend actions
    /// • Communicate directly with Scout
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// Scout
    ///   └── MusicExpert
    ///         └── ArtSpecialist
    /// -------------------------------------------------------------------------
    /// </summary>
    public class ArtSpecialist : ObservationSpecialist
    {
        private static readonly HashSet<string> ArtworkNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "cover.jpg",
                "cover.jpeg",
                "cover.png",
                "folder.jpg",
                "folder.jpeg",
                "folder.png",
                "front.jpg",
                "front.jpeg",
                "front.png"
            };

        public override string Name => "Artwork Recognition";

        public override string Summary =>
            "Detects common album artwork stored alongside music files.";

        public override ExpertFinding Observe(
            IReadOnlyList<FileContext> files)
        {
            var artworkFiles = files
                .Where(IsArtworkFile)
                .ToList();

            if (artworkFiles.Count == 0)
            {
                return new ExpertFinding
                {
                    FoundSomething = false,
                    Summary = "No folder artwork detected."
                };
            }

            var finding = new ExpertFinding
            {
                FoundSomething = true,
                Summary = $"Detected artwork in {artworkFiles.Count} folder(s)."
            };

            foreach (var artwork in artworkFiles)
            {
                finding.Evidence.Add(
                    $"{Path.GetFileName(artwork.ParentFolder)} → {artwork.OriginalName}");
            }

            finding.Questions.Add("Embedded artwork has not yet been analyzed.");
            finding.Questions.Add("Artwork quality has not yet been evaluated.");

            return finding;
        }

        private static bool IsArtworkFile(FileContext file)
        {
            return ArtworkNames.Contains(
                Path.GetFileName(file.OriginalName));
        }
    }
}