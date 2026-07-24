using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.BuildingBlocks;
using SmartRenamer.Observations.Insights;
using SmartRenamer.Observations.Specialists;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// =========================================================================
    /// MusicExpert
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Understand the music library before recommending improvements."
    ///
    /// Domain
    /// -------------------------------------------------------------------------
    /// Music Collections
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// MusicExpert understands music collections by combining the observations
    /// made by its Specialists. Each Specialist investigates one aspect of the
    /// collection while MusicExpert synthesizes those findings into a broader
    /// understanding.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate music Specialists
    /// • Collect Specialist findings
    /// • Develop an overall understanding
    /// • Report observations to Scout
    /// =========================================================================
    /// </summary>
    public class MusicExpert : ObservationExpert
    {
        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
        [
            new AlbumSpecialist(),
            new ArtSpecialist(),
            new MetadataSpecialist()
        ];

        public override IReadOnlyList<ObservationSpecialist> Specialists => _specialists;

        public override string Name => "Music Collection";

        public override string Summary =>
            "I noticed music files that appear to belong together.";

        public override string WhyItMatters =>
            "Keeping related music together makes it easier to browse, enjoy, and preserve your music library.";

        public override List<ExpertFinding> Observe(IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = base.Observe(files);

            ExpertInsight insight = BuildInsight(findings);

            // Future versions of Scout will consume this insight.
            _ = insight;

            return findings;
        }

        private ExpertInsight BuildInsight(IReadOnlyList<ExpertFinding> findings)
        {
            FindingAnalyzer findingAnalyzer = new(findings);
            SignalAnalyzer signalAnalyzer = new(findings);

            ExpertInsight insight = new()
            {
                Summary =
                    $"I combined {findingAnalyzer.Count()} observations to better understand this music collection.",

                Confidence =
                    findingAnalyzer.HasFindings() ? 1.0 : 0.5
            };

            insight.Evidence.AddRange(findingAnalyzer.GetEvidence());
            insight.Questions.AddRange(findingAnalyzer.GetQuestions());

            // Signals are intentionally unused for now.
            // Future versions of Scout will use them to prioritize
            // observations and recommendations.
            _ = signalAnalyzer.GetSignals();

            return insight;
        }
    }
}