using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// =========================================================================
/// ConversationPlanner
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Guide the expedition."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Coordinates the conversation between Scout and the user.
///
/// Responsibilities
/// -------------------------------------------------------------------------
/// • Receive ExpertFindings from all Experts.
/// • Determine the most valuable recommendation.
/// • Ask the next question.
/// • Adapt the conversation based on user responses.
/// • Coordinate the Navigation, Evidence, and Conversation panels.
/// • Maintain the current expedition.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Analyze files.
/// • Know anything about ebooks, music, documents, or other domains.
/// • Render the UI.
/// • Modify files.
///
/// Those responsibilities belong to Experts and the User Interface.
/// =========================================================================


namespace Scout.Observations.Conversation
{
    public sealed class CV_ConversationPlanner
    {
    }
}
/// =========================================================================
/// CV_ConversationPlanner
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Guide the expedition."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Coordinates the conversation between Scout and the user.
///
/// Future Responsibilities
/// -------------------------------------------------------------------------
/// • Receive ExpertFindings from all Experts.
/// • Determine the most valuable recommendation.
/// • Ask the next question.
/// • Adapt the conversation based on user responses.
/// • Coordinate the Navigation, Evidence, and Conversation panels.
/// • Maintain the current expedition.
/// • Ensure every recommendation is supported by evidence.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Analyze files.
/// • Know anything about ebooks, music, documents, or other domains.
/// • Render the UI.
/// • Modify files.
///
/// Those responsibilities belong to Experts and the User Interface.
///
/// Relationship to Scout
/// -------------------------------------------------------------------------
/// Experts
///      ↓
/// ExpertFindings
///      ↓
/// CV_ConversationPlanner
///      ↓
/// Current Recommendation
///      ↓
/// User Interface
/// =========================================================================