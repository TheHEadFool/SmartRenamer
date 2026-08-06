using Scout.Observations.Conversation;
using SmartRenamer.Observations;

namespace SmartRenamer.Observations.Experts.EbookExpert.Translators;

/// <summary>
/// =========================================================================
/// E_RecommendationTranslator
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Translate ebook expertise into conversation."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Converts Ebook ExpertFindings into user-friendly conversation
/// recommendations that Scout can discuss with the user.
///
/// Future Responsibilities
/// -------------------------------------------------------------------------
/// • Translate ExpertFindings into CV_Recommendations.
/// • Preserve evidence.
/// • Preserve confidence.
/// • Explain recommendations in user-friendly language.
/// • Generate questions Scout can ask the user.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Analyze files.
/// • Decide which recommendation is most important.
/// • Render the user interface.
///
/// Those responsibilities belong to the Ebook Expert,
/// Conversation Planner, and User Interface.
/// =========================================================================
/// </summary>
public sealed class E_RecommendationTranslator
{
    /// <summary>
    /// Converts a single ExpertFinding into a Conversation Recommendation.
    /// </summary>
    public CV_Recommendation Translate(
        ExpertFinding finding)
    {
        CV_Recommendation recommendation = new()
        {
            // For now, the ExpertFinding summary becomes the
            // conversational reason. As the architecture evolves,
            // richer recommendation properties will be translated here.
            Reason = finding.Summary
        };

        //---------------------------------------------------------
        // Preserve supporting evidence.
        //---------------------------------------------------------

        recommendation.Evidence.AddRange(
            finding.Evidence);

        return recommendation;
    }
}