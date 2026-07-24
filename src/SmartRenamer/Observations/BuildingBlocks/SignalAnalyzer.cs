using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Observations.Signals;

namespace SmartRenamer.Observations.BuildingBlocks
{
    /// <summary>
    /// =========================================================================
    /// SignalAnalyzer
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Reason from observations, not sentences."
    ///
    /// Domain
    /// -------------------------------------------------------------------------
    /// Observation Signals
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Provides convenient methods for working with ObservationSignals
    /// collected from one or more ExpertFindings.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// Experts should focus on interpreting observations rather than
    /// searching through collections of findings.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Query ObservationSignals
    /// • Count ObservationSignals
    /// • Hide traversal logic
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Produce observations
    /// • Generate insights
    /// • Modify findings
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// ObservationSpecialists
    ///         ↓
    ///     ExpertFindings
    ///         ↓
    ///    SignalAnalyzer
    ///         ↓
    ///      MusicExpert
    /// =========================================================================
    /// </summary>
    public class SignalAnalyzer
    {
        private readonly IReadOnlyList<ExpertFinding> _findings;

        public SignalAnalyzer(IReadOnlyList<ExpertFinding> findings)
        {
            _findings = findings;
        }

        /// <summary>
        /// Returns true if any finding contains the specified signal.
        /// </summary>
        public bool HasSignal(ObservationSignal signal)
        {
            return _findings.Any(f => f.Signals.Contains(signal));
        }

        /// <summary>
        /// Counts how many findings contain the specified signal.
        /// </summary>
        public int CountSignal(ObservationSignal signal)
        {
            return _findings.Count(f => f.Signals.Contains(signal));
        }

        /// <summary>
        /// Returns every ObservationSignal from every finding.
        /// </summary>
        public IEnumerable<ObservationSignal> GetSignals()
        {
            return _findings.SelectMany(f => f.Signals);
        }
    }
}