using Scout.Observations.Conversation;
using SmartRenamer.Models;
using SmartRenamer.Observations;
using SmartRenamer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartRenamer.Guide
{
    /// <summary>
    /// Connects Scout to the Intelligence Engine.
    ///
    /// Scout doesn't investigate folders directly.
    /// Scout asks the workflow to investigate,
    /// analyze, and prepare a rename preview.
    ///
    /// Folder selection can also be performed separately so the
    /// Conversation layer can collect required user choices before
    /// investigation begins.
    /// </summary>
    public class GuideInvestigator
    {
        private readonly ProjectWorkflow workflow = new();

        private readonly ProjectInvestigator projectInvestigator = new();

        /// <summary>
        /// Exposes the generic discovery choices supplied by the
        /// Observation Experts through the workflow boundary.
        ///
        /// The Guide does not interpret these choices. It only exposes
        /// the workflow's generic discovery contract to the conversation
        /// layer.
        /// </summary>
        public IReadOnlyList<ExpertDiscoveryBinding> DiscoveryBindings =>
            workflow.DiscoveryBindings;

        /// <summary>
        /// Exposes recommendations produced by a re-observation that
        /// followed a successful action.
        ///
        /// The Guide does not interpret these recommendations. It only
        /// exposes the workflow's generic conversation contract.
        /// </summary>
        public IReadOnlyList<CV_Recommendation> ReobservationRecommendations =>
            workflow.LastReobservationRecommendations;

        /// <summary>
        /// Applies a discovery choice through the same generic workflow
        /// boundary used by the Observation Engine.
        ///
        /// The Guide does not know what the option means. The owning
        /// Observation Expert interprets the option.
        /// </summary>
        public void ApplyDiscoveryChoice(
            string expertName,
            string optionId)
        {
            workflow.ApplyDiscoveryChoice(
                expertName,
                optionId);
        }

        //---------------------------------------------------------
        // Generic Expert Decisions
        //---------------------------------------------------------

        /// <summary>
        /// Exposes the generic decision requests currently supplied by
        /// the Observation Experts through the workflow boundary.
        ///
        /// Decisions occur after investigation and are distinct from
        /// discovery choices. The Guide does not interpret their meaning.
        /// It only exposes the generic decision contract to the
        /// conversation layer.
        /// </summary>
        public IReadOnlyList<ExpertDecisionRequest> DecisionRequests =>
            workflow.DecisionRequests;

        /// <summary>
        /// Exposes bindings between generic decision requests and the
        /// Experts that own them.
        ///
        /// The Guide does not interpret the requests or their options.
        /// The owning Expert remains responsible for their meaning.
        /// </summary>
        public IReadOnlyList<ExpertDecisionBinding> DecisionBindings =>
            workflow.DecisionBindings;

        /// <summary>
        /// Applies a user-selected decision through the generic workflow
        /// boundary.
        ///
        /// The Guide does not know what the option means. The owning
        /// Observation Expert interprets the opaque option identifier.
        /// </summary>
        public void ApplyDecisionChoice(
            string expertName,
            string optionId)
        {
            workflow.ApplyDecisionChoice(
                expertName,
                optionId);
        }

        /// <summary>
        /// Lets the user select a folder without beginning investigation.
        ///
        /// The Guide can use this to ask required questions before
        /// the Intelligence Engine begins its investigation.
        /// </summary>
        public string? PickFolder()
        {
            return projectInvestigator.PickFolder();
        }

        /// <summary>
        /// Investigates a folder that has already been selected.
        ///
        /// The folder is scanned and converted into a ProjectContext
        /// first. The existing ProjectWorkflow then performs the normal
        /// intelligence workflow against that context.
        /// </summary>
        public WorkflowResult? Investigate(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return null;

            ProjectContext? context =
                projectInvestigator.Investigate(folder);

            if (context == null)
                return null;

            return workflow.Execute(context);
        }

        /// <summary>
        /// Runs the complete intelligence workflow using the existing
        /// folder-selection path.
        ///
        /// Preserved so existing callers continue to work while the
        /// conversation-driven folder-selection path is introduced.
        /// </summary>
        public WorkflowResult? Investigate()
        {
            return workflow.Execute();
        }

        /// <summary>
        /// Executes a Conversation Framework action through the same
        /// ProjectWorkflow that performed the investigation.
        ///
        /// This preserves the domain Expert state created during
        /// investigation.
        /// </summary>
        public CV_ActionResult ExecuteAction(
            CV_ActionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return workflow.ExecuteAction(request);
        }

        /// <summary>
        /// Converts the workflow results into
        /// conversational language.
        /// </summary>
        public string Summarize(WorkflowResult result)
        {
            if (result == null || result.Project == null)
            {
                return "I wasn't able to analyze that folder.";
            }

            ProjectContext context = result.Project;

            StringBuilder summary = new();

            summary.AppendLine("I've finished analyzing your folder.");
            summary.AppendLine();

            int renameCount = result.Preview.Count;

            if (renameCount == 0)
            {
                summary.AppendLine(
                    "I didn't find any filenames that need changing.");
            }
            else if (renameCount == 1)
            {
                summary.AppendLine(
                    "I found 1 filename that could be improved.");
            }
            else
            {
                summary.AppendLine(
                    $"I found {renameCount:N0} filenames that could be improved.");
            }

            summary.AppendLine();

            if (context.Observations.Count > 0)
            {
                summary.AppendLine("Here's what I noticed:");

                foreach (ProjectObservation observation in context.Observations)
                {
                    summary.AppendLine($"• {observation.Description}");
                }

                summary.AppendLine();
            }

            if (context.RecommendedCapabilities.Count > 0)
            {
                summary.AppendLine("My recommendations:");

                foreach (string capability in context.RecommendedCapabilities)
                {
                    summary.AppendLine($"• {capability}");
                }

                summary.AppendLine();
            }

            if (renameCount == 0)
            {
                summary.AppendLine(
                    "There isn't anything I'd recommend renaming right now.");
            }
            else
            {
                summary.AppendLine(
                    "I've already prepared a preview of the proposed filename changes.");

                summary.AppendLine();

                summary.AppendLine(
                    "We can refine those changes together before anything is renamed.");

                summary.AppendLine();

                summary.AppendLine(
                    "What would you like me to do?");
            }

            return summary.ToString();
        }
    }
}