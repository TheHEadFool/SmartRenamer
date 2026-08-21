using System;
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
    /// "Capture what an Investigation discovered."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents a single finding produced by an Investigation.
    ///
    /// A finding contains both human-readable information and structured
    /// ObservationSignals that Scout can reason about.
    ///
    /// ExpertFindings are the primary communication contract between
    /// Investigations and the rest of the Scout system.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Identify the finding.
    /// • Describe what was discovered.
    /// • Record supporting evidence.
    /// • Record unanswered questions.
    /// • Provide structured ObservationSignals.
    /// • Provide a consistent communication contract for the UI.
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// Block
    ///   ↓
    /// Report
    ///   ↓
    /// Consultant
    ///   ↓
    /// ExpertFinding
    ///   ↓
    /// ObservationExpert
    ///   ↓
    /// Scout
    /// =========================================================================
    ///
    /// Design Principles
    /// -------------------------------------------------------------------------
    /// Every ExpertFinding should answer four questions:
    ///
    /// • What was discovered?      (Summary)
    /// • Why was it reported?      (Evidence)
    /// • How certain is it?        (Confidence)
    /// • What should happen next?  (Questions)
    ///
    /// These principles help ensure every Investigation communicates in a
    /// consistent manner regardless of domain.
    ///
    /// =========================================================================
    /// FINDING ID
    /// =========================================================================
    ///
    /// Every finding receives a stable identity when it is created.
    ///
    /// The identity allows the same underlying discovery to be connected
    /// across the Observation Framework, Conversation Framework, Review All
    /// report, and Workspace UI.
    ///
    /// This is important because the same finding may appear in several
    /// places:
    ///
    ///     ExpertFinding
    ///          ↓
    ///     ProjectObservation
    ///          ↓
    ///     CV_Recommendation
    ///          ↓
    ///     CV_ReviewAllItem
    ///
    /// These are different representations of the SAME discovery.
    ///
    /// The finding ID prevents those representations from becoming separate
    /// or duplicated discoveries.
    /// =========================================================================
    /// </summary>
    public class ExpertFinding
    {
        /// <summary>
        /// Stable identity for this finding.
        ///
        /// The same finding identity allows the Observation Framework,
        /// Conversation Framework, Review All report, and Workspace UI to
        /// refer to the same underlying discovery without creating duplicate
        /// findings.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Did this Investigation discover anything worth reporting?
        /// </summary>
        public bool FoundSomething { get; init; }

        /// <summary>
        /// A concise human-readable summary of the finding.
        /// This becomes the primary headline shown by Scout.
        /// </summary>
        public string Summary { get; init; } = string.Empty;

        /// <summary>
        /// Confidence from 0.0 to 1.0.
        /// </summary>
        public double Confidence { get; init; }

        /// <summary>
        /// Supporting facts that explain why this finding was reported.
        ///
        /// Intended primarily for human consumption.
        /// </summary>
        public List<string> Evidence { get; } = new();

        /// <summary>
        /// Structured observations that Scout uses for reasoning.
        ///
        /// Intended primarily for software rather than people.
        /// </summary>
        public List<ObservationSignal> Signals { get; } = new();

        /// <summary>
        /// Additional questions or follow-up investigations suggested by
        /// this finding.
        /// </summary>
        public List<string> Questions { get; } = new();
    }
}