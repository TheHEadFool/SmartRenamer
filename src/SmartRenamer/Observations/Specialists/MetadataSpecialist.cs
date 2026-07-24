using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Models;
using SmartRenamer.Observations.BuildingBlocks;
using SmartRenamer.Observations.Signals;

namespace SmartRenamer.Observations.Specialists
{
    /// <summary>
    /// =========================================================================
    /// MetadataSpecialist
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Understand the identity of every song before judging the library."
    ///
    /// Domain
    /// -------------------------------------------------------------------------
    /// Music Metadata
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Reads music metadata and reports factual observations that help
    /// MusicExpert understand the quality of a music collection.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// Metadata identifies music. Without artist, album, title and track
    /// information a music library is difficult to organize correctly.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Read metadata
    /// • Delegate counting to MetadataAnalyzer
    /// • Report observations
    /// • Emit ObservationSignals
    /// • Ask unanswered questions
    ///
    /// This Specialist does NOT
    /// -------------------------------------------------------------------------
    /// • Organize music
    /// • Modify metadata
    /// • Recommend fixes
    /// • Communicate directly with Scout
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// Scout
    ///   └── MusicExpert
    ///         └── MetadataSpecialist
    ///               ├── MusicMetadataReader
    ///               └── MetadataAnalyzer
    /// =========================================================================
    /// </summary>
    public class MetadataSpecialist : ObservationSpecialist
    {
        public override string Name => "Metadata Analysis";

        public override string Summary =>
            "Analyzes descriptive information stored in music files.";

        public override ExpertFinding Observe(
            IReadOnlyList<FileContext> files)
        {
            var analyzer = new MetadataAnalyzer();

            foreach (var file in files.Where(IsMusicFile))
            {
                var tag = MusicMetadataReader.Read(file);

                if (tag == null)
                    continue;

                analyzer.CountFile();

                if (HasArtist(tag))
                    analyzer.CountArtist();

                if (HasAlbum(tag))
                    analyzer.CountAlbum();

                if (HasTitle(tag))
                    analyzer.CountTitle();

                if (HasTrack(tag))
                    analyzer.CountTrack();
            }

            if (analyzer.MusicFiles == 0)
            {
                return new ExpertFinding
                {
                    FoundSomething = false,
                    Summary = "No readable music metadata found."
                };
            }

            var finding = new ExpertFinding
            {
                FoundSomething = true,
                Summary = $"Read metadata from {analyzer.MusicFiles} music files."
            };

            finding.Signals.Add(ObservationSignal.MetadataPresent);

            finding.Evidence.Add($"Artist: {analyzer.ArtistCount}/{analyzer.MusicFiles}");
            finding.Evidence.Add($"Album: {analyzer.AlbumCount}/{analyzer.MusicFiles}");
            finding.Evidence.Add($"Title: {analyzer.TitleCount}/{analyzer.MusicFiles}");
            finding.Evidence.Add($"Track Number: {analyzer.TrackCount}/{analyzer.MusicFiles}");

            if (analyzer.ArtistCount < analyzer.MusicFiles)
            {
                finding.Signals.Add(ObservationSignal.MissingArtistMetadata);
                finding.Questions.Add("Some files are missing Artist metadata.");
            }

            if (analyzer.AlbumCount < analyzer.MusicFiles)
            {
                finding.Signals.Add(ObservationSignal.MissingAlbumMetadata);
                finding.Questions.Add("Some files are missing Album metadata.");
            }

            if (analyzer.TitleCount < analyzer.MusicFiles)
            {
                finding.Signals.Add(ObservationSignal.MissingTitleMetadata);
                finding.Questions.Add("Some files are missing Title metadata.");
            }

            if (analyzer.TrackCount < analyzer.MusicFiles)
            {
                finding.Signals.Add(ObservationSignal.MissingTrackMetadata);
                finding.Questions.Add("Some files are missing Track Numbers.");
            }

            return finding;
        }

        private static bool IsMusicFile(FileContext file)
        {
            switch (file.Extension.ToLowerInvariant())
            {
                case ".mp3":
                case ".flac":
                case ".wav":
                case ".aac":
                case ".m4a":
                case ".ogg":
                case ".wma":
                case ".opus":
                case ".aiff":
                case ".alac":
                    return true;

                default:
                    return false;
            }
        }

        private static bool HasArtist(TagLib.Tag tag) =>
            tag.Performers.Length > 0;

        private static bool HasAlbum(TagLib.Tag tag) =>
            !string.IsNullOrWhiteSpace(tag.Album);

        private static bool HasTitle(TagLib.Tag tag) =>
            !string.IsNullOrWhiteSpace(tag.Title);

        private static bool HasTrack(TagLib.Tag tag) =>
            tag.Track > 0;
    }
}