using System.Collections.Generic;
using System.Linq;

namespace SmartRenamer.Observations.BuildingBlocks
{
    /// <summary>
    /// =========================================================================
    /// FindingAnalyzer
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Gather what Specialists discovered."
    ///
    /// Domain
    /// -------------------------------------------------------------------------
    /// Expert Findings
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Provides convenient access to the combined evidence and questions
    /// produced by a collection of ExpertFindings.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// Experts should focus on interpreting findings rather than traversing
    /// collections and aggregating their contents.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Aggregate evidence
    /// • Aggregate questions
    /// • Count findings
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Produce findings
    /// • Interpret findings
    /// • Generate insights
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// ObservationSpecialists
    ///         ↓
    ///     ExpertFindings
    ///         ↓
    ///    FindingAnalyzer
    ///         ↓
    ///      MusicExpert
    /// =========================================================================
    /// </summary>
    public class FindingAnalyzer
    {
        private readonly IReadOnlyList<ExpertFinding> _findings;

        public FindingAnalyzer(IReadOnlyList<ExpertFinding> findings)
        {
            _findings = findings;
        }

        /// <summary>
        /// Returns every piece of evidence from every finding.
        /// </summary>
        public IEnumerable<string> GetEvidence()
        {
            return _findings.SelectMany(f => f.Evidence);
        }

        /// <summary>
        /// Returns every unanswered question from every finding.
        /// </summary>
        public IEnumerable<string> GetQuestions()
        {
            return _findings.SelectMany(f => f.Questions);
        }

        /// <summary>
        /// Returns the number of findings.
        /// </summary>
        public int Count()
        {
            return _findings.Count;
        }

        /// <summary>
        /// Returns true if any Specialist reported a finding.
        /// </summary>
        public bool HasFindings()
        {
            return _findings.Any(f => f.FoundSomething);
        }
    }
}