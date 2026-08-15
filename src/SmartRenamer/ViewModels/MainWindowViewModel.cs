using Scout.Core;
using Scout.Observations.Conversation;
using SmartRenamer.Capabilities;
using SmartRenamer.Guide;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Recommendations;
using SmartRenamer.Models.Rename;
using SmartRenamer.Services;
using SmartRenamer.ViewModels.Guide;
using SmartRenamer.ViewModels.Workspace;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartRenamer.ViewModels
{
    /// <summary>
    /// =========================================================================
    /// MainWindowViewModel
    /// =========================================================================
    ///
    /// Domain:
    /// -------------------------------------------------------------------------
    /// Application Coordination
    ///
    /// Purpose:
    /// -------------------------------------------------------------------------
    /// Coordinates the primary SmartRenamer window and connects the Guide,
    /// workflow, Observation Framework, Conversation Framework, Workspace,
    /// and active Scout Expedition.
    ///
    /// Responsibilities:
    /// -------------------------------------------------------------------------
    /// • Receive completed workflows from the Guide.
    /// • Preserve the existing recommendation/action pipeline.
    /// • Pass Expert-generated CV_Recommendations to the Conversation Engine.
    /// • Expose the selected conversation topic to the Workspace.
    /// • Provide access to the active Scout Expedition.
    /// • Coordinate approved execution.
    ///
    /// This class does NOT:
    /// -------------------------------------------------------------------------
    /// • Analyze files.
    /// • Interpret ExpertFindings.
    /// • Create Expert recommendations.
    /// • Decide which domain recommendation is best.
    /// • Define Safari-specific presentation.
    ///
    /// Those responsibilities belong to the appropriate workflow,
    /// Observation, Expert, Translator, Conversation, and Expedition
    /// components.
    ///
    /// Conversation Integration:
    /// -------------------------------------------------------------------------
    /// The Conversation Engine receives the recommendations produced by the
    /// Observation Framework. It selects the current recommendation, and this
    /// ViewModel makes that topic available to the Workspace.
    ///
    /// Expedition Integration:
    /// -------------------------------------------------------------------------
    /// The active Expedition is owned by the application-level
    /// ExpeditionManager. This ViewModel exposes that existing manager to the
    /// application UI without creating a second Expedition instance.
    ///
    /// The ViewModel does not decide which Expedition is active.
    /// It does not load Expedition resources.
    /// It does not contain Safari-specific logic.
    ///
    /// The existing RecommendationBuilder remains in place while the new
    /// Expert-driven pipeline is being proven.
    /// =========================================================================
    /// </summary>
    public class MainWindowViewModel : ObservableObject
    {
        //---------------------------------------------------------
        // Current Workflow
        //---------------------------------------------------------

        private WorkflowResult? currentWorkflow;

        //---------------------------------------------------------
        // Legacy Recommendation Pipeline
        //---------------------------------------------------------

        private readonly SmartRenamer.Guide.RecommendationBuilder
            recommendationBuilder = new();

        //---------------------------------------------------------
        // Services
        //---------------------------------------------------------

        private readonly RenameService renameService = new();

        private readonly ScoutService scoutService = new();

        private readonly ScoutOperation operation = new();

        //---------------------------------------------------------
        // Conversation Framework
        //---------------------------------------------------------

        /// <summary>
        /// Coordinates the new Expert-driven conversation pipeline.
        /// </summary>
        private readonly CV_ConversationEngine conversationEngine = new();

        //---------------------------------------------------------
        // Expedition
        //---------------------------------------------------------

        /// <summary>
        /// Provides access to the application-owned ExpeditionManager.
        ///
        /// The ExpeditionManager is created and initialized by App.
        /// This property does not create another manager and does not select
        /// the active Expedition.
        /// </summary>
        public ExpeditionManager ExpeditionManager =>
            ((App)Application.Current).ExpeditionManager;

        /// <summary>
        /// Provides the manifest for the currently active Expedition.
        ///
        /// This is a convenience property for the UI layer. The
        /// ExpeditionManager remains the authoritative owner of the manifest.
        /// </summary>
        public Scout.Core.Expeditions.ExpeditionManifest? CurrentExpedition =>
            ExpeditionManager.CurrentManifest;

        //---------------------------------------------------------
        // Collections
        //---------------------------------------------------------

        public ObservableCollection<RenameItem> Files { get; }
            = new();

        //---------------------------------------------------------
        // ViewModels
        //---------------------------------------------------------

        public GuideViewModel Guide { get; }
            = new();

        public PipelineViewModel Pipeline { get; }
            = new();

        public ProjectWorkspaceViewModel Workspace { get; }
            = new();

        //---------------------------------------------------------
        // Operation
        //---------------------------------------------------------

        public ScoutOperation Operation =>
            operation;

        //---------------------------------------------------------
        // Commands
        //---------------------------------------------------------

        public RelayCommand AddFilesCommand { get; }

        public RelayCommand PreviewCommand { get; }

        public RelayCommand RenameCommand { get; }

        public RelayCommand ExecuteRecommendationCommand { get; }

        //---------------------------------------------------------
        // Constructor
        //---------------------------------------------------------

        public MainWindowViewModel()
        {
            AddFilesCommand =
                new RelayCommand(AddFiles);

            PreviewCommand =
                new RelayCommand(Preview);

            RenameCommand =
                new RelayCommand(Rename);

            ExecuteRecommendationCommand =
                new RelayCommand(ExecuteRecommendation);

            Pipeline.AddStep(
                new WorkflowStep(
                    new ChooseFolderStep()));

            Guide.ProjectCreated +=
                Guide_ProjectCreated;

            Guide.PlanApproved +=
                Guide_PlanApproved;
        }

        //---------------------------------------------------------
        // Project Created
        //---------------------------------------------------------

        private void Guide_ProjectCreated(
            object? sender,
            WorkflowResult result)
        {
            currentWorkflow = result;

            LoadConversation(result);
        }

        //---------------------------------------------------------
        // Plan Approved
        //---------------------------------------------------------

        private void Guide_PlanApproved(
            object? sender,
            EventArgs e)
        {
            if (currentWorkflow == null)
            {
                MessageBox.Show(
                    "No active project.",
                    "Scout");

                return;
            }

            operation.Title =
                "Organizing Files";

            operation.Status =
                "Preparing an organized copy...";

            operation.CurrentTask =
                "Scanning files...";

            operation.State =
                ScoutOperationState.Running;

            operation.CompletedSteps =
                0;

            operation.CurrentFile =
                "Waiting...";

            RenameResult result =
                scoutService.Execute(
                    currentWorkflow,
                    operation);

            if (result.Success)
            {
                string message =
                    "Scout successfully created an organized copy of your project.\n\n" +
                    $"Files copied: {result.FilesRenamed:N0} file(s)\n\n";

                if (!string.IsNullOrWhiteSpace(
                        result.OutputFolder))
                {
                    message +=
                        $"Location:\n{result.OutputFolder}\n\n";
                }

                message +=
                    "Your original files were not modified.";

                MessageBox.Show(
                    message,
                    "Organization Complete");

                ProjectWorkflow workflow =
                    new();

                currentWorkflow =
                    workflow.Execute(
                        currentWorkflow.Project);

                LoadConversation(
                    currentWorkflow);
            }
            else
            {
                MessageBox.Show(
                    $"Renamed {result.FilesRenamed:N0} file(s).\n\n" +
                    string.Join(
                        "\n",
                        result.Errors),
                    "Rename Complete");
            }
        }

        //---------------------------------------------------------
        // Conversation
        //---------------------------------------------------------

        /// <summary>
        /// Loads both recommendation paths for the current workflow.
        ///
        /// Legacy Recommendation objects continue to populate the existing
        /// action panel.
        ///
        /// Expert-generated CV_Recommendations are passed through the
        /// Conversation Framework so Scout can begin discussing the selected
        /// recommendation.
        /// </summary>
        private void LoadConversation(
            WorkflowResult workflow)
        {
            if (workflow == null)
                return;

            //---------------------------------------------------------
            // New Expert-driven Conversation Pipeline
            //---------------------------------------------------------

            //---------------------------------------------------------
            // TEMPORARY LIMITATION — CONVERSATION SELECTION
            //---------------------------------------------------------
            //
            // The Conversation Engine currently receives the complete set of
            // CV_Recommendations, but CV_RecommendationSelector intentionally
            // selects the first recommendation.
            //
            // This is deliberate for the initial Conversation Framework
            // vertical slice. The purpose of this implementation is to prove
            // that an Expert-generated CV_Recommendation can travel:
            //
            //     Expert
            //       ↓
            //     CV_Recommendation
            //       ↓
            //     CV_ConversationEngine
            //       ↓
            //     CV_CurrentTopic
            //       ↓
            //     Workspace
            //
            // DO NOT add prioritization, ranking, scoring, or conversational
            // selection logic here.
            //
            // Future work should improve CV_RecommendationSelector when the
            // Conversation Framework is ready for recommendation prioritization.
            //
            // Until then, "first recommendation wins" is the known and
            // intentional behavior.
            //
            // This comment should be removed or updated when the Selector
            // becomes responsible for real recommendation selection.
            //
            //---------------------------------------------------------

            conversationEngine.Start(
                workflow.ObservationRecommendations);

            //---------------------------------------------------------
            // Expose the Conversation Engine's current topic
            // through the Workspace.
            //---------------------------------------------------------

            Workspace.CurrentTopic.Clear();

            CV_Recommendation? currentRecommendation =
                conversationEngine.CurrentTopic.Recommendation;

            if (currentRecommendation != null)
            {
                Workspace.CurrentTopic.Begin(
                    currentRecommendation);
            }

            //---------------------------------------------------------
            // Existing Recommendation Pipeline
            //
            // Keep this intact while the new Observation Framework
            // is being proven.
            //---------------------------------------------------------

            Workspace.Load(
                workflow,
                recommendationBuilder.Build(
                    workflow));
        }

        //---------------------------------------------------------
        // Legacy Recommendation Execution
        //---------------------------------------------------------

        private void ExecuteRecommendation(
            object? parameter)
        {
            if (parameter is not Recommendation recommendation)
                return;

            switch (recommendation.ActionId)
            {
                case "ReviewPreview":

                    MessageBox.Show(
                        $"Scout is ready to review {Workspace.RenameCount} proposed filename change(s).",
                        "Scout");

                    break;

                case "RenameFiles":

                    Guide_PlanApproved(
                        this,
                        EventArgs.Empty);

                    break;

                case "ExplainChanges":

                    MessageBox.Show(
                        "Explanation mode isn't implemented yet.",
                        "Scout");

                    break;

                default:

                    MessageBox.Show(
                        $"Unknown action: {recommendation.ActionId}",
                        "Scout");

                    break;
            }
        }

        //---------------------------------------------------------
        // Existing Commands
        //---------------------------------------------------------

        private void AddFiles()
        {
        }

        private void Preview()
        {
        }

        private void Rename()
        {
        }
    }
}