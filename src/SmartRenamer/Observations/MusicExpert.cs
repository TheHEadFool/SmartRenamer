using System.Collections.Generic;
using Scout.Observations.Conversation;
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
    /// made by its Specialists.
    ///
    /// Each Specialist investigates one aspect of the collection.
    /// MusicExpert combines those findings into a broader understanding.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate music Specialists.
    /// • Collect Specialist findings.
    /// • Develop an overall understanding.
    /// • Translate findings into conversation recommendations.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Render the user interface.
    /// • Manage the conversation.
    /// • Execute file operations.
    ///
    /// =========================================================================
    /// CURRENT PROJECT STATUS
    /// =========================================================================
    ///
    /// MusicExpert is currently being migrated to the Generation 2
    /// ObservationExpert contract.
    ///
    /// IMPORTANT:
    /// -------------------------------------------------------------------------
    /// This is intentionally a MINIMUM migration.
    ///
    /// We are NOT completing MusicExpert at this stage.
    ///
    /// The purpose of this migration is to:
    ///
    /// • Preserve the existing Music Specialist work.
    /// • Make MusicExpert compatible with the new ObservationExpert contract.
    /// • Allow the solution to build.
    /// • Allow EbookExpert to become the current reference Expert.
    ///
    /// MusicExpert will later be rebuilt through Scout's Expert-creation
    /// process as the proof that Scout can create a complete Expert from
    /// an interview and domain-information package.
    ///
    /// FUTURE MUSIC EXPERT WORK
    /// -------------------------------------------------------------------------
    /// The following areas remain intentionally incomplete:
    ///
    /// • Rich music recommendation translation.
    /// • Full music-library insight synthesis.
    /// • Additional music Specialists.
    /// • User-question generation based on music-specific goals.
    /// • Music Expert Factory generation.
    ///
    /// Do NOT add speculative functionality here while EbookExpert is being
    /// completed.
    ///
    /// =========================================================================
    /// </summary>
    public class MusicExpert : ObservationExpert
    {
        //---------------------------------------------------------
        // Specialists
        //---------------------------------------------------------

        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
        [
            new AlbumSpecialist(),
            new ArtSpecialist(),
            new MetadataSpecialist()
        ];

        public override IReadOnlyList<ObservationSpecialist> Specialists =>
            _specialists;

        //---------------------------------------------------------
        // Identity
        //---------------------------------------------------------

        public override string Name =>
            "Music Collection";

        public override string Summary =>
            "I noticed music files that appear to belong together.";

        public override string WhyItMatters =>
            "Keeping related music together makes it easier to browse, enjoy, and preserve your music library.";

        //---------------------------------------------------------
        // Generation 2 Investigation
        //---------------------------------------------------------

        /// <summary>
        /// Runs the existing Music Specialists and returns their findings.
        ///
        /// This replaces the former Observe() override.
        ///
        /// The Specialist implementations themselves are preserved.
        /// This method simply provides the new ObservationExpert entry point.
        /// </summary>
        public override List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Preserve existing Specialist behavior.
            //---------------------------------------------------------

            foreach (ObservationSpecialist specialist in Specialists)
            {
                ExpertFinding finding =
                    specialist.Observe(files);

                if (finding == null)
                    continue;

                findings.Add(finding);
            }

            //---------------------------------------------------------
            // Preserve the existing Music insight work.
            //
            // The insight is intentionally not exposed yet.
            // EbookExpert is currently establishing the Generation 2
            // pattern that the completed Music Expert will eventually
            // follow.
            //---------------------------------------------------------

            ExpertInsight insight =
                BuildInsight(findings);

            // --------------------------------------------------------
            // FUTURE:
            // The completed Music Expert will expose structured insight
            // through the common Scout observation pipeline.
            //
            // For this migration, preserve the existing work without
            // inventing a new integration point.
            // --------------------------------------------------------

            _ = insight;

            return findings;
        }

        //---------------------------------------------------------
        // Generation 2 Recommendation Translation
        //---------------------------------------------------------

        /// <summary>
        /// Converts Music Expert findings into the conversation recommendation
        /// contract required by ObservationEngine.
        ///
        /// This is intentionally minimal.
        ///
        /// MusicExpert will receive a dedicated, richer recommendation
        /// translator when MusicExpert is rebuilt through the Expert Factory
        /// process.
        /// </summary>
        public override List<CV_Recommendation> BuildRecommendations(
            IReadOnlyList<ExpertFinding> findings)
        {
            List<CV_Recommendation> recommendations = new();

            foreach (ExpertFinding finding in findings)
            {
                if (finding == null)
                    continue;

                //---------------------------------------------------------
                // Minimal Generation 2 translation.
                //
                // Preserve the factual finding without inventing
                // music-specific recommendations that we have not
                // designed yet.
                //---------------------------------------------------------

                CV_Recommendation recommendation = new()
                {
                    Title =
                        string.IsNullOrWhiteSpace(finding.Summary)
                            ? "Music Library Observation"
                            : finding.Summary,

                    Question =
                        finding.Questions.Count > 0
                            ? finding.Questions[0]
                            : string.Empty,

                    Reason =
                        finding.Summary,

                    SafetyMessage =
                        "Scout has not changed your files."
                };

                recommendation.Evidence.AddRange(
                    finding.Evidence);

                if (finding.Questions.Count > 0)
                {
                    recommendation.Benefits.Add(
                        "Scout can investigate this further before recommending changes.");
                }

                recommendations.Add(recommendation);
            }

            return recommendations;
        }

        //---------------------------------------------------------
        // Existing Music Insight
        //---------------------------------------------------------

        /// <summary>
        /// Combines the findings produced by the Music Specialists into
        /// an overall Music Expert insight.
        ///
        /// This existing work is deliberately preserved during the
        /// Generation 2 migration.
        /// </summary>
        private ExpertInsight BuildInsight(
            IReadOnlyList<ExpertFinding> findings)
        {
            FindingAnalyzer findingAnalyzer =
                new(findings);

            SignalAnalyzer signalAnalyzer =
                new(findings);

            ExpertInsight insight = new()
            {
                Summary =
                    $"I combined {findingAnalyzer.Count()} observations to better understand this music collection.",

                Confidence =
                    findingAnalyzer.HasFindings()
                        ? 1.0
                        : 0.5
            };

            insight.Evidence.AddRange(
                findingAnalyzer.GetEvidence());

            insight.Questions.AddRange(
                findingAnalyzer.GetQuestions());

            //---------------------------------------------------------
            // Signals are intentionally preserved but unused for now.
            //
            // Future MusicExpert work will use these signals to reason
            // about music-library conditions and determine what Scout
            // should ask next.
            //---------------------------------------------------------

            _ = signalAnalyzer.GetSignals();

            return insight;
        }
    }
}