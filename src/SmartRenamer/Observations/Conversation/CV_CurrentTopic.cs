using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scout.Observations.Conversation
{
    internal class CV_CurrentTopic
    {
    }
}
/// =========================================================================
/// CV_CurrentTopic
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Stay focused on the current goal."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Represents the recommendation Scout is currently discussing with the user.
///
/// Future Responsibilities
/// -------------------------------------------------------------------------
/// • Track the current recommendation.
/// • Provide the evidence supporting that recommendation.
/// • Know whether the recommendation has been accepted,
///   postponed, or declined.
/// • Allow Scout to naturally change topics during the expedition.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Decide what recommendation comes next.
/// • Analyze files.
/// • Render the UI.
///
/// Those responsibilities belong to the Conversation Planner,
/// Experts, and the User Interface.
/// =========================================================================