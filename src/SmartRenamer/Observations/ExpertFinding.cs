using System.Collections.Generic;
using SmartRenamer.Observations.Signals;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// =========================================================================
    /// ExpertFinding
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Capture what a Specialist discovered."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents a single finding produced by an ObservationSpecialist.
    /// A finding contains both human-readable information and structured
    /// ObservationSignals that Experts can reason about.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Describe what was discovered
    /// • Record supporting evidence
    /// • Record unanswered questions
    /// • Provide structured ObservationSignals
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// ObservationSpecialist
    ///         ↓
    ///     ExpertFinding
    ///         ↓
    ///     MusicExpert
    ///         ↓
    ///    ExpertInsight
    /// =========================================================================
    /// </summary>
    public class ExpertFinding
    {
        /// <summary>
        /// Did this Specialist discover anything worth reporting?
        /// </summary>
        public bool FoundSomething { get; init; }

        /// <summary>
        /// One-sentence summary for the Expert.
        /// </summary>
        public string Summary { get; init; } = string.Empty;

        /// <summary>
        /// Confidence from 0.0 to 1.0.
        /// </summary>
        public double Confidence { get; init; }

        /// <summary>
        /// Facts observed while investigating.
        /// Intended for human consumption.
        /// </summary>
        public List<string> Evidence { get; } = new();

        /// <summary>
        /// Structured observations that Experts use for reasoning.
        /// Intended for software rather than people.
        /// </summary>
        public List<ObservationSignal> Signals { get; } = new();

        /// <summary>
        /// Additional things this Specialist would like to investigate later.
        /// </summary>
        public List<string> Questions { get; } = new();
    }
}