using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scout.Observations.Conversation
{
    public sealed class CV_Recommendation
    {
        public string Title { get; init; } = string.Empty;

        public string Question { get; init; } = string.Empty;

        public string Reason { get; init; } = string.Empty;

        public List<string> Evidence { get; } = new();

        public string SafetyMessage { get; init; } = string.Empty;

        public List<string> Benefits { get; } = new();

        public string EstimatedTime { get; init; } = string.Empty;
    }
}
/// =========================================================================
/// CV_Recommendation
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Recommend the next best step."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Represents one recommendation Scout would like to discuss with the user.
///
/// Future Responsibilities
/// -------------------------------------------------------------------------
/// • Describe the recommendation.
/// • Explain why it is valuable.
/// • Record confidence.
/// • Provide supporting evidence.
/// • Suggest the next question Scout should ask.
/// • Track its current status.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Decide whether it should be selected.
/// • Analyze files.
/// • Communicate directly with the user.
///
/// Those responsibilities belong to the Conversation Planner
/// and the Experts.
/// =========================================================================