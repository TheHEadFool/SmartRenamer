using Scout.Observations.Conversation;
using SmartRenamer.Controls.ConversationCards;
using SmartRenamer.Guide;
using SmartRenamer.Guide.Models;
using SmartRenamer.Guide.Thinking;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Rename;
using SmartRenamer.Observations;
using SmartRenamer.Services;
using SmartRenamer.ViewModels.Workspace;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRenamer.ViewModels.Guide
{
    /// <summary>
    /// =========================================================================
    /// GuideViewModel
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates Scout's conversation with the user.
    ///
    /// The Guide is responsible for conversation presentation and user intent
    /// handling. Domain Experts remain responsible for domain knowledge and
    /// domain operations.
    ///
    /// =========================================================================
    /// CONVERSATION ARCHITECTURE
    /// =========================================================================
    ///
    /// The current architecture is:
    ///
    ///     ObservationEngine
    ///          ↓
    ///     ExpertFindings
    ///          ↓
    ///     CV_Recommendation
    ///          ↓
    ///     Workspace ConversationEngine
    ///          ↓
    ///     Guide
    ///          ↓
    ///     CV_ActionRequest
    ///          ↓
    ///     Domain Action Dispatcher
    ///
    /// The Guide must not reach directly into the ObservationEngine.
    ///
    /// The Guide also does not interpret domain-specific action meaning.
    ///
    /// =========================================================================
    /// MIGRATION STATUS
    /// =========================================================================
    ///
    /// The new Conversation Framework is authoritative for investigation
    /// conversations.
    ///
    /// The older ScoutConversationEngine remains temporarily for the legacy
    /// rename workflow until those responsibilities have been absorbed by the
    /// newer Conversation Framework.
    ///
    /// =========================================================================
    /// </summary>
    public class GuideViewModel : ObservableObject
    {
        //---------------------------------------------------------
        // Legacy rename conversation support
        //---------------------------------------------------------

        private readonly GuideInvestigator guideInvestigator = new();

        private readonly ScoutThoughtBuilder thoughtBuilder = new();

        private readonly ScoutConversationEngine conversationEngine = new();

        //---------------------------------------------------------
        // Workspace
        //---------------------------------------------------------

        private readonly ProjectWorkspaceViewModel workspace;

        //---------------------------------------------------------
        // Conversation State
        //---------------------------------------------------------

        private ConversationStage stage =
            ConversationStage.Greeting;

        public GuideConversation Conversation { get; } =
            new();

        /// <summary>
        /// The actions currently available to the user.
        /// These are presented by the Action Button Bar.
        /// The Guide does not interpret their domain meaning.
        /// </summary>
        public ObservableCollection<CV_ActionOption> ActionOptions { get; } =
            new();

        //---------------------------------------------------------
        // Events
        //---------------------------------------------------------

        public event EventHandler<WorkflowResult>? ProjectCreated;

        public event EventHandler? PlanApproved;

        public event EventHandler? ReviewAllRequested;

        //---------------------------------------------------------
        // Current Workflow
        //---------------------------------------------------------

        private WorkflowResult? currentWorkflow;

        // ---------------------------------------------------------
        // Discovery Choice State
        // ---------------------------------------------------------
        //
        // The Guide holds only the generic discovery information supplied
        // by the workflow. It does not interpret what the choices mean.
        // ---------------------------------------------------------

        private string? selectedFolder;

        private readonly List<ExpertDiscoveryBinding> pendingDiscoveryBindings =
            new();

        private int pendingDiscoveryIndex;

        private bool awaitingDiscoveryChoice;

        //---------------------------------------------------------
        // User Input
        //---------------------------------------------------------

        private string userInput = "";

        public string UserInput
        {
            get => userInput;

            set => SetProperty(
                ref userInput,
                value);
        }

        //---------------------------------------------------------
        // Commands
        //---------------------------------------------------------

        public RelayCommand SendCommand { get; }

        public RelayCommand BrowseFolderCommand { get; }

        public RelayCommand SelectActionOptionCommand { get; }
        public RelayCommand ExecuteRecommendationActionCommand { get; }

        // =====================================================================
        // Constructor
        // =====================================================================

        public GuideViewModel(
            ProjectWorkspaceViewModel workspace)
        {
            this.workspace =
                workspace ??
                throw new ArgumentNullException(
                    nameof(workspace));

            workspace.ConversationMessageGenerated +=
                Workspace_ConversationMessageGenerated;

            SendCommand =
                new RelayCommand(Send);

            BrowseFolderCommand =
                new RelayCommand(ChooseFolder);

            SelectActionOptionCommand =
    new RelayCommand(parameter =>
    {
        if (parameter is CV_ActionOption option)
            SelectActionOption(option);
    });
            ExecuteRecommendationActionCommand =
    new RelayCommand(parameter =>
    {
        if (parameter is CV_Recommendation recommendation)
            ExecuteRecommendationAction(recommendation);
    });
            //---------------------------------------------------------
            // Initial folder picker card.
            //---------------------------------------------------------

            Conversation.Messages.Add(
                new GuideMessage
                {
                    IsGuide = true,

                    Card = new FolderPickerCard
                    {
                        Command =
                            BrowseFolderCommand
                    }
                });
        }

        // =====================================================================
        // Workspace Conversation Messages
        // =====================================================================

        private void Workspace_ConversationMessageGenerated(
            object? sender,
            CV_ConversationMessage message)
        {
            if (message == null)
                return;

            if (string.IsNullOrWhiteSpace(message.Text))
                return;

            Conversation.AddGuideMessage(
                message.Text);
        }

        // =====================================================================
        // Legacy Question Support
        // =====================================================================

        private void AskNextQuestion()
        {
            List<ScoutThought> thoughts =
                thoughtBuilder.Build(
                    new ProjectContext());

            ScoutQuestion? question =
                conversationEngine.GetNextQuestion(
                    thoughts);

            if (question != null)
            {
                Conversation.AddGuideMessage(
                    question.Text);
            }
        }

        // =====================================================================
        // Send
        // =====================================================================

        private async void Send()
        {

            if (string.IsNullOrWhiteSpace(UserInput))
                return;

            string answer =
                UserInput.Trim();

            Conversation.AddUserMessage(
                answer);

            UserInput = "";

            if (awaitingDiscoveryChoice)
            {
                if (TryApplyTypedDiscoveryChoice(answer))
                    return;

                Conversation.AddGuideMessage(
                    "Please choose one of the discovery options shown above, or type its exact label.");

                return;
            }

            // =================================================================
            // NEW CONVERSATION FRAMEWORK
            // =================================================================
            //
            // Investigation conversations are owned by the Workspace's
            // authoritative Conversation Engine.
            //
            // The Guide:
            //
            //     • receives the user's answer
            //     • asks the Conversation Engine to interpret it
            //     • asks whether a domain action should be created
            //     • presents the resulting conversation
            //
            // The Guide does NOT execute an ObservationEngine operation.
            //
            // Domain action execution will be connected through the dedicated
            // action bridge rather than adding an ObservationEngine dependency
            // to ProjectWorkspaceViewModel.
            // =================================================================

            if (stage ==
                ConversationStage.InvestigationConversation)
            {
                CV_UserIntent userIntent =
    workspace.ConversationEngine
        .InterpretUserInput(answer);

                if (workspace.ConversationEngine.IsAwaitingReviewAllApproval)
                {
                    if (userIntent.Type == CV_UserIntentType.Approve)
                    {
                        workspace.ConversationEngine.ClearReviewAllPrompt();

                        ReviewAllRequested?.Invoke(
                            this,
                            EventArgs.Empty);

                        Conversation.AddGuideMessage(
                            "I'll review all of the recommendations for you.");

                        return;
                    }

                    workspace.ConversationEngine.ClearReviewAllPrompt();
                }

                CV_ActionRequest? actionRequest =
                    workspace.ConversationEngine
                        .CreateActionRequest(answer);

                // -------------------------------------------------------------
                // A recommendation has produced a concrete domain action.
                //
                // The Conversation Framework creates the request.
                //
                // The domain action dispatcher will execute it.
                //
                // The Guide does not know what the action means.
                // -------------------------------------------------------------

                if (actionRequest != null)
                {
                    Conversation.AddGuideMessage("");

                    //---------------------------------------------------------
                    // The Conversation Framework has created a concrete
                    // domain action request.
                    //
                    // The Guide does not know what the action means.
                    // It passes the request to the GuideInvestigator, which
                    // routes it through the same ProjectWorkflow used for
                    // the investigation.
                    //---------------------------------------------------------

                    CV_ActionResult actionResult =
                        await ExecuteActionAsync(
                            actionRequest);

                    //---------------------------------------------------------
                    // Report a failed action.
                    //---------------------------------------------------------

                    HandleActionResult(actionResult);
                    return;

                }

                // -------------------------------------------------------------
                // No executable action was generated.
                //
                // Continue handling conversational intents that do not
                // require a domain action.
                // -------------------------------------------------------------

                switch (userIntent.Type)
                {
                    case CV_UserIntentType.Approve:

                        Conversation.AddGuideMessage("");

                        Conversation.AddGuideMessage(
                            "I understand. You want me to proceed with this recommendation.");

                        break;

                    case CV_UserIntentType.Research:

                        Conversation.AddGuideMessage("");

                        Conversation.AddGuideMessage(
                            "I understand. You want me to research the missing information.");

                        Conversation.AddGuideMessage(
                            "The research request has been recognized, but no executable research action was created.");

                        break;

                    case CV_UserIntentType.ReviewAll:

                        ReviewAllRequested?.Invoke(
                            this,
                            EventArgs.Empty);

                        Conversation.AddGuideMessage(
                            "I'll review all of the recommendations for you.");

                        break;

                    default:

                        Conversation.AddGuideMessage(
                            "I understand that we're discussing the findings from the investigation.");

                        Conversation.AddGuideMessage(
                            "You can ask me to research missing information or approve the recommendation.");

                        break;
                }

                return;
            }

            // =================================================================
            // LEGACY CONVERSATION FRAMEWORK
            // =================================================================
            //
            // This remains temporarily for the older rename workflow.
            //
            // The InvestigationConversation stage above belongs exclusively
            // to the newer Conversation Framework.
            //
            // =================================================================

            conversationEngine.ProcessAnswer(
                answer);

            if (answer.Equals(
                "review all",
                StringComparison.OrdinalIgnoreCase))
            {
                ReviewAllRequested?.Invoke(
                    this,
                    EventArgs.Empty);

                Conversation.AddGuideMessage(
                    "I'll review all of the recommendations for you.");

                return;
            }

            switch (stage)
            {
                case ConversationStage.Greeting:

                    Conversation.AddGuideMessage("");

                    Conversation.AddGuideMessage(
                        "Opening the folder browser...");

                    ChooseFolder();

                    break;

                case ConversationStage.ChooseFolder:

                    //---------------------------------------------------------
                    // This stage is no longer used because Scout automatically
                    // opens the folder browser.
                    //---------------------------------------------------------

                    break;

                case ConversationStage.ReviewPlan:

                    switch (
                        conversationEngine.GetIntent(
                            answer))
                    {
                        case ConversationIntent.Approve:

                            if (currentWorkflow == null)
                            {
                                Conversation.AddGuideMessage(
                                    "I don't have a preview to rename.");

                                break;
                            }

                            Conversation.AddGuideMessage("");

                            Conversation.AddGuideMessage(
                                "Great! I'll start applying the changes.");

                            PlanApproved?.Invoke(
                                this,
                                EventArgs.Empty);

                            break;

                        case ConversationIntent.Help:

                            Conversation.AddGuideMessage("");

                            Conversation.AddGuideMessage(
                                "Here's what I'm doing:");

                            Conversation.AddGuideMessage(
                                "• I investigated your folder.");

                            Conversation.AddGuideMessage(
                                "• I created a preview so nothing changes until you approve it.");

                            Conversation.AddGuideMessage(
                                "• If you'd like something different, just tell me how you'd like the filenames changed.");

                            Conversation.AddGuideMessage(
                                "Nothing will be renamed until you approve the preview.");

                            break;

                        case ConversationIntent.Refine:

                            Conversation.AddGuideMessage("");

                            Conversation.AddGuideMessage(
                                "I understand what you'd like to change.");

                            Conversation.AddGuideMessage(
                                "Refining the preview isn't available yet.");

                            Conversation.AddGuideMessage(
                                "That's the next capability I'll learn.");

                            break;

                        case ConversationIntent.Cancel:

                            Conversation.AddGuideMessage("");

                            Conversation.AddGuideMessage(
                                "No problem.");

                            Conversation.AddGuideMessage(
                                "We can continue whenever you're ready.");

                            break;

                        default:

                            Conversation.AddGuideMessage(
                                "I'm not sure what you'd like me to do.");

                            Conversation.AddGuideMessage(
                                "You can approve the preview, ask me to explain it, ask me to change it, or cancel.");

                            break;
                    }

                    break;
            }
        }

        // =====================================================================
        // Discovery Choice Handling
        // =====================================================================

        /// <summary>
        /// Handles a discovery choice supplied by the conversation layer.
        ///
        /// The Guide does not interpret the choice. It routes the Expert name
        /// and option identifier through GuideInvestigator so the owning Expert
        /// can apply its own meaning.
        /// </summary>
        public void SelectDiscoveryOption(
            GuideInlineAction action)
        {
            if (action == null)
                return;

            ApplyDiscoveryChoice(
                action.ActionId);
        }

        /// <summary>
        /// Applies a typed or clicked discovery choice and advances the
        /// discovery sequence. Typed answers and direct selections converge
        /// here so the underlying workflow receives the same choice.
        /// </summary>
        private void ApplyDiscoveryChoice(
            string optionId)
        {
            if (!awaitingDiscoveryChoice)
                return;

            if (pendingDiscoveryIndex < 0 ||
                pendingDiscoveryIndex >= pendingDiscoveryBindings.Count)
            {
                awaitingDiscoveryChoice = false;
                return;
            }

            ExpertDiscoveryBinding binding =
                pendingDiscoveryBindings[pendingDiscoveryIndex];

            guideInvestigator.ApplyDiscoveryChoice(
                binding.Expert.Name,
                optionId);

            pendingDiscoveryIndex++;

            if (pendingDiscoveryIndex < pendingDiscoveryBindings.Count)
            {
                PresentDiscoveryQuestion();
                return;
            }

            awaitingDiscoveryChoice = false;
            pendingDiscoveryBindings.Clear();
            pendingDiscoveryIndex = 0;

            if (!string.IsNullOrWhiteSpace(selectedFolder))
            {
                string folder = selectedFolder;
                selectedFolder = null;
                InvestigateSelectedFolder(folder);
            }
        }

        /// <summary>
        /// Accepts a typed discovery answer. Matching is deliberately limited
        /// to the options supplied by the current Expert.
        /// </summary>
        private bool TryApplyTypedDiscoveryChoice(
            string answer)
        {
            if (!awaitingDiscoveryChoice)
                return false;

            if (pendingDiscoveryIndex < 0 ||
                pendingDiscoveryIndex >= pendingDiscoveryBindings.Count)
            {
                return false;
            }

            ExpertDiscoveryBinding binding =
                pendingDiscoveryBindings[pendingDiscoveryIndex];

            ExpertDiscoveryOption? option =
                binding.Request.Options.FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Id,
                            answer,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            candidate.Label,
                            answer,
                            StringComparison.OrdinalIgnoreCase));

            if (option == null)
                return false;

            ApplyDiscoveryChoice(option.Id);
            return true;
        }

        /// <summary>
        /// Presents the next generic discovery question supplied by an Expert.
        /// The Guide does not know what the options mean.
        /// </summary>
        private void PresentDiscoveryQuestion()
        {
            if (pendingDiscoveryIndex < 0 ||
                pendingDiscoveryIndex >= pendingDiscoveryBindings.Count)
            {
                awaitingDiscoveryChoice = false;
                return;
            }

            ExpertDiscoveryBinding binding =
                pendingDiscoveryBindings[pendingDiscoveryIndex];

            Conversation.AddGuideMessage(
                "Before I investigate, I need one choice about how you'd like me to search.");

            foreach (ExpertDiscoveryOption option
                in binding.Request.Options)
            {
                Conversation.Messages.Add(
                    new GuideMessage
                    {
                        Speaker = GuideSpeaker.Guide,
                        DisplayName = "Scout",
                        Text = option.Label,
                        Payload = new GuideInlineAction(
                            option.Label,
                            option.Id)
                    });
            }
        }

        /// <summary>
        /// Starts the generic discovery-choice sequence for the selected folder.
        /// If no Expert requires a choice, investigation begins immediately.
        /// </summary>
        private void BeginDiscovery(
            string folder)
        {
            selectedFolder = folder;

            pendingDiscoveryBindings.Clear();
            pendingDiscoveryBindings.AddRange(
                guideInvestigator.DiscoveryBindings
                    .Where(binding =>
                        binding.Request.Options.Count > 0));

            pendingDiscoveryIndex = 0;

            if (pendingDiscoveryBindings.Count == 0)
            {
                selectedFolder = null;
                InvestigateSelectedFolder(folder);
                return;
            }

            awaitingDiscoveryChoice = true;
            PresentDiscoveryQuestion();
        }

        /// <summary>
        /// Begins the existing investigation workflow after all required
        /// discovery choices have been supplied.
        /// </summary>
        private void InvestigateSelectedFolder(
            string folder)
        {
            WorkflowResult? result =
                guideInvestigator.Investigate(folder);

            CompleteInvestigation(result);
        }

        /// <summary>
        /// Completes the existing Guide behavior after investigation.
        /// </summary>
        /// <summary>
        /// Completes the existing Guide behavior after investigation.
        ///
        /// The investigation produces conversation recommendations through
        /// the workflow. Those recommendations must be loaded into the
        /// Workspace's authoritative Conversation Engine before the Guide
        /// attempts to continue the investigation conversation.
        ///
        /// This is the handoff between:
        ///
        ///     Investigation
        ///          ↓
        ///     Recommendations
        ///          ↓
        ///     Workspace Conversation Engine
        ///          ↓
        ///     Guide
        ///
        /// The Guide does not interpret the recommendation or its domain
        /// meaning. It simply establishes the authoritative conversation
        /// state and asks the engine to discuss the first recommendation.
        /// </summary>
        private void CompleteInvestigation(
            WorkflowResult? result)
        {
            if (result == null)
            {
                Conversation.AddGuideMessage(
                    "No folder was selected.");

                stage =
                    ConversationStage.Greeting;

                return;
            }

            currentWorkflow =
                result;

            stage =
                ConversationStage.InvestigationConversation;

            ProjectCreated?.Invoke(
                this,
                result);

            ProjectObservation? firstObservation =
                result.Project.Observations
                    .FirstOrDefault();

            int observationCount =
                result.Project.Observations.Count;

            if (firstObservation != null)
            {
                Conversation.AddGuideMessage(
                    $"I explored your folder and found {observationCount} things worth looking at. " +
                    $"One thing that stood out was {firstObservation.Title.ToLower()}.");
            }
            else
            {
                Conversation.AddGuideMessage(
                    $"I explored your folder and found {observationCount} things worth looking at.");
            }

            int proposedChanges =
                result.Preview.Count(
                    p => p.HasChanges);

            Conversation.AddGuideMessage(
                proposedChanges > 0
                    ? $"I also prepared a safe preview showing {proposedChanges} proposed organizational changes. Nothing has been changed."
                    : "I prepared a safe preview, and nothing has been changed.");

            // -------------------------------------------------------------
            // Establish the Workspace Conversation Engine as the
            // authoritative owner of the investigation conversation.
            //
            // ProjectWorkflow has already translated the Expert findings
            // into these recommendations. Loading them here ensures the
            // engine used by Guide is working with the same result that
            // was just investigated.
            // -------------------------------------------------------------

            workspace.ConversationEngine.LoadRecommendations(
                result.ObservationRecommendations);

            // -------------------------------------------------------------
            // Start the conversation with the first recommendation.
            //
            // DiscussRecommendation creates the actual conversation
            // message. Guide is already subscribed to the Workspace
            // ConversationMessageGenerated event, so the message is
            // presented through the normal conversation path.
            // -------------------------------------------------------------

            CV_Recommendation? firstRecommendation =
                result.ObservationRecommendations
                    .FirstOrDefault();

            if (firstRecommendation != null)
            {
                CV_ConversationMessage? message =
                    workspace.ConversationEngine
                        .DiscussRecommendation(
                            firstRecommendation);

                if (message != null &&
                    !string.IsNullOrWhiteSpace(message.Text))
                {
                    Conversation.AddGuideMessage(
                        message.Text);
                }
            }
        }

        /// <summary>
        /// Executes a conversation action option selected by clicking
        /// the option displayed in the conversation.
        ///
        /// Clicking is simply another way of expressing the user's choice.
        /// It uses the same Conversation Framework action path as typed input.
        /// </summary>
        public async void SelectActionOption(
            CV_ActionOption option)

        {
            if (option == null)
                return;

            CV_ActionRequest? actionRequest =
                workspace.ConversationEngine.CreateActionRequest(
                    option.Id);

            if (actionRequest == null)
            {
                Conversation.AddGuideMessage(
                    "I couldn't process that selection.");

                return;
            }

            workspace.ConversationEngine.ClearActionOptions();

            ActionOptions.Clear();

            Conversation.AddUserMessage(
                option.Label);

            CV_ActionResult actionResult =
                await ExecuteActionAsync(
                    actionRequest);

            HandleActionResult(actionResult);
        }

        /// <summary>
        /// Executes the action associated with the currently selected
        /// recommendation.
        ///
        /// Clicking the recommendation action uses the same Conversation
        /// Framework action-request path as typed approval. The Guide does
        /// not interpret the domain meaning of the action.
        /// </summary>
        public async void ExecuteRecommendationAction(
            CV_Recommendation recommendation)
        {
            if (recommendation == null)
                return;

            CV_ActionRequest? actionRequest =
                workspace.ConversationEngine.CreateActionRequest(
                    recommendation);

            if (actionRequest == null)
            {
                Conversation.AddGuideMessage(
                    "I couldn't process that action.");

                return;
            }

            Conversation.AddUserMessage(
                recommendation.ActionText);

            CV_ActionResult actionResult =
                await ExecuteActionAsync(
                    actionRequest);

            HandleActionResult(actionResult);
        }

        // =====================================================================
        // Action Execution
        // =====================================================================

        /// <summary>
        /// Executes a domain action away from the WPF UI thread.
        ///
        /// The domain workflow is intentionally left synchronous. The Guide
        /// moves the potentially long-running operation off the UI thread and
        /// resumes on the UI thread when the action has completed so the
        /// conversation can safely update the presentation.
        /// </summary>
        private async Task<CV_ActionResult> ExecuteActionAsync(
            CV_ActionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return await Task.Run(
                () => guideInvestigator.ExecuteAction(request));
        }

        // =====================================================================
        // Action Result Handling
        // =====================================================================

        /// <summary>
        /// Handles the result of a domain action in one common place.
        ///
        /// Both typed user input and clicked action options arrive here.
        /// This keeps conversation handling centralized so future workflow
        /// cycles can continue from the same point.
        /// </summary>
        private void HandleActionResult(CV_ActionResult actionResult)
        {
            if (actionResult == null)
                return;

            // -------------------------------------------------------------
            // Report a failed action.
            // -------------------------------------------------------------

            if (!actionResult.Success)
            {
                Conversation.AddGuideMessage(
                    string.IsNullOrWhiteSpace(actionResult.Message)
                        ? "I wasn't able to complete that action."
                        : actionResult.Message);

                return;
            }

            // -------------------------------------------------------------
            // Report the result returned by the domain Expert.
            // -------------------------------------------------------------

            if (!string.IsNullOrWhiteSpace(actionResult.Message))
            {
                Conversation.AddGuideMessage(
                    actionResult.Message);
            }

            // -------------------------------------------------------------
            // Report supporting evidence.
            // -------------------------------------------------------------

            foreach (string evidence in actionResult.Evidence)
            {
                if (!string.IsNullOrWhiteSpace(evidence))
                {
                    Conversation.AddGuideMessage(
                        evidence);
                }
            }

            // -------------------------------------------------------------
            // Present structured options.
            //
            // These may be ISBN candidates, cover choices, or other
            // domain-specific choices supplied by the Expert.
            // -------------------------------------------------------------

            workspace.ConversationEngine.RememberActionOptions(
    actionResult.Options);

            ActionOptions.Clear();

            foreach (CV_ActionOption option
                in actionResult.Options)
            {
                Conversation.Messages.Add(
                    new GuideMessage
                    {
                        Speaker = GuideSpeaker.Guide,
                        DisplayName = "Scout",
                        Payload = option
                    });

                ActionOptions.Add(option);
            }

            // -------------------------------------------------------------
            // Continue the investigation after an action that requires
            // re-observation.
            //
            // ProjectWorkflow performs the re-observation and retains the
            // resulting recommendations. GuideInvestigator exposes those
            // recommendations without interpreting their domain meaning.
            // The Workspace Conversation Engine then becomes the normal
            // presentation path for the next recommendation.
            // -------------------------------------------------------------

            if (actionResult.RequiresReobservation)
            {
                IReadOnlyList<CV_Recommendation> recommendations =
                    guideInvestigator.ReobservationRecommendations;

                workspace.ConversationEngine.ClearActionOptions();
                ActionOptions.Clear();

                if (recommendations.Count > 0)
                {
                    workspace.ConversationEngine.LoadRecommendations(
                        recommendations);

                    CV_Recommendation firstRecommendation =
                        recommendations[0];

                    CV_ConversationMessage? message =
                        workspace.ConversationEngine
                            .DiscussRecommendation(
                                firstRecommendation);

                    if (message != null &&
                        !string.IsNullOrWhiteSpace(message.Text))
                    {
                        Conversation.AddGuideMessage(
                            message.Text);
                    }
                }
            }
        }

        // =====================================================================
        // Choose Folder
        // =====================================================================

        private void ChooseFolder()
        {
            System.Diagnostics.Debug.WriteLine(
                "ChooseFolder() called.");

            string? folder =
                guideInvestigator.PickFolder();

            if (string.IsNullOrWhiteSpace(folder))
            {
                Conversation.AddGuideMessage(
                    "No folder was selected.");

                stage =
                    ConversationStage.Greeting;

                return;
            }

            BeginDiscovery(folder);
        }
    }
}
