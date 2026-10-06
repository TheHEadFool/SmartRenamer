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
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

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

        private GuideInvestigator guideInvestigator;

        private readonly ScoutOperation operation;

        private readonly IProgress<ExecutionProgress> investigationProgress;

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
        /// Operational actions currently available to the user.
        /// These are presented in the single persistent Scout Controls area.
        /// The Guide does not interpret their domain meaning.
        /// </summary>
        public ObservableCollection<CV_ActionOption> ActionOptions { get; } =
            new();

        /// <summary>
        /// Generic Expert decisions currently available as persistent choices.
        /// These mirror the inline decision links so the user does not have to
        /// scroll back through the conversation to answer the current question.
        /// </summary>
        public ObservableCollection<GuideInlineAction> DecisionOptions { get; } =
            new();

        /// <summary>
        /// Generic Expert discovery choices currently available to the user.
        /// These are rendered in the same persistent Scout Controls area as
        /// decisions and action options.
        /// </summary>
        public ObservableCollection<GuideInlineAction> DiscoveryOptions { get; } =
            new();

        /// <summary>
        /// True when Scout Controls has at least one real user action.
        /// Conversation explains what Scout is doing; this is the single
        /// persistent action surface.
        /// </summary>
        public bool HasScoutControls =>
            DecisionOptions.Count > 0 ||
            DiscoveryOptions.Count > 0 ||
            ActionOptions.Count > 0;

        /// <summary>
        /// Show the older observation buttons only when they are actually the
        /// current navigation surface. Once Scout has a concrete current
        /// action/decision, those older observations become stale clutter.
        /// Organization is likewise a decision stage, so the old observation
        /// buttons are hidden there.
        /// </summary>
        public bool ShowWorkspaceObservations =>
            !HasScoutControls &&
            !operation.HasNeedsItems &&
            !operation.HasWorkingItems &&
            !string.Equals(
                operation.Stage,
                "Organization",
                StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Plain-language direction for a beginner. It deliberately points to
        /// the persistent Scout Controls area instead of sending the user back
        /// into the conversation transcript to find an old button.
        /// </summary>
        public string ScoutControlsPrompt
        {
            get
            {
                if (DiscoveryOptions.Count > 0)
                    return "Choose one of the buttons below to tell Scout how to begin.";

                if (DecisionOptions.Count > 0)
                    return "Scout needs your choice. Use one of the buttons below to continue.";

                if (ActionOptions.Count > 0)
                    return "Scout needs your attention. Use one of the buttons below.";

                return string.Empty;
            }
        }

        private void NotifyScoutControlsChanged()
        {
            OnPropertyChanged(nameof(HasScoutControls));
            OnPropertyChanged(nameof(ShowWorkspaceObservations));
            OnPropertyChanged(nameof(ScoutControlsPrompt));
        }

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

        // ---------------------------------------------------------
        // Generic Expert Decision State
        // ---------------------------------------------------------
        //
        // Decisions are separate from discovery. The Guide only transports
        // the request and the user's response; the owning Expert interprets
        // the option and any optional value.
        // ---------------------------------------------------------

        private readonly List<ExpertDecisionBinding> pendingDecisionBindings =
            new();

        private int pendingDecisionIndex;

        private bool awaitingDecisionChoice;

        // ---------------------------------------------------------
        // Reversible decision history
        // ---------------------------------------------------------
        //
        // The Guide records only the generic identity of an applied decision.
        // The owning Expert remains responsible for restoring its domain state.
        // ---------------------------------------------------------

        private sealed class AppliedDecision
        {
            public string ExpertName { get; init; } = string.Empty;
        }

        private readonly Stack<AppliedDecision> decisionHistory =
            new();

        private bool canGoBack;

        // True after a source folder has been selected and remains true
        // until the current expedition is reset. Expert decision history
        // still controls whether Back rewinds an Expert decision or resets
        // the expedition.
        private bool expeditionActive;

        public bool CanGoBack
        {
            get => canGoBack;
            private set => SetProperty(
                ref canGoBack,
                value);
        }

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

        public RelayCommand BackCommand { get; }

        public RelayCommand BrowseFolderCommand { get; }

        public RelayCommand SelectActionOptionCommand { get; }
        public RelayCommand ExecuteRecommendationActionCommand { get; }
        public RelayCommand ExecuteProgressActionCommand { get; }
        public RelayCommand AcceptAllAsIsCommand { get; }
        public RelayCommand SelectDecisionOptionCommand { get; }
        public RelayCommand SelectDiscoveryOptionCommand { get; }
        public RelayCommand ToggleAutomaticRepairsCommand { get; }

        private bool automaticRepairsEnabled = true;

        /// <summary>
        /// Presentation state for the expedition-scoped automatic-repair
        /// authorization. The Guide never performs the repair itself; the
        /// toggle creates the same Conversation Framework action used by
        /// recommendations and typed conversation.
        /// </summary>
        public bool AutomaticRepairsEnabled
        {
            get => automaticRepairsEnabled;
            private set => SetProperty(
                ref automaticRepairsEnabled,
                value);
        }

        // =====================================================================
        // Constructor
        // =====================================================================

        public GuideViewModel(
            ProjectWorkspaceViewModel workspace,
            ScoutOperation operation)
        {
            this.workspace =
                workspace ??
                throw new ArgumentNullException(
                    nameof(workspace));

            this.operation =
                operation ??
                throw new ArgumentNullException(
                    nameof(operation));

            investigationProgress =
                new Progress<ExecutionProgress>(p =>
                {
                    operation.CompletedSteps = p.Completed;
                    operation.TotalSteps = p.Total;
                    operation.CurrentFile = p.CurrentFile;
                    operation.Status = p.Status;
                    operation.Stage = p.Stage;
                    OnPropertyChanged(nameof(ShowWorkspaceObservations));
                    operation.StageCompleted = p.StageCompleted;
                    operation.StageTotal = p.StageTotal;
                    operation.CollectionTotal = p.CollectionTotal;
                    operation.CollectionCompleted = p.CollectionCompleted;
                    operation.CollectionProcessing = p.CollectionProcessing;
                    operation.CollectionWaiting = p.CollectionWaiting;
                    operation.CollectionPending = p.CollectionPending;
                    operation.ApplyItems(p.Items);
                    OnPropertyChanged(nameof(ShowWorkspaceObservations));

                    if (p.CollectionProcessing > 0 &&
                        string.Equals(
                            p.Stage,
                            "Repair",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        operation.Status =
                            $"Scout is researching {p.CollectionProcessing:N0} book{(p.CollectionProcessing == 1 ? "" : "s")} in the background. You do not need to wait; if research cannot complete, the book will move to NEEDS.";
                    }
                    else if (p.CollectionWaiting > 0 &&
                             string.Equals(
                                 p.Stage,
                                 "Repair",
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        operation.Status =
                            "Scout is waiting for your decision on one or more books.";
                    }

                    operation.CurrentTask =
                        p.CollectionTotal > 0
                            ? $"{p.Stage} — {p.CollectionCompleted:N0}/{p.CollectionTotal:N0} complete"
                            : p.Total > 0
                                ? $"Investigating {p.Completed:N0} of {p.Total:N0}"
                                : "Investigating...";
                });

            guideInvestigator =
                new GuideInvestigator(investigationProgress);

            guideInvestigator.BackgroundActionCompleted +=
                GuideInvestigator_BackgroundActionCompleted;

            workspace.ConversationMessageGenerated +=
                Workspace_ConversationMessageGenerated;

            SendCommand =
                new RelayCommand(Send);

            BackCommand =
                new RelayCommand(GoBack);

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

            ExecuteProgressActionCommand =
                new RelayCommand(parameter =>
                {
                    if (parameter is ExecutionProgressAction action)
                        ExecuteProgressAction(action);
                });

            AcceptAllAsIsCommand =
                new RelayCommand(AcceptAllAsIs);

            SelectDecisionOptionCommand =
                new RelayCommand(parameter =>
                {
                    if (parameter is GuideInlineAction action)
                        SelectDecisionOption(action);
                });

            SelectDiscoveryOptionCommand =
                new RelayCommand(parameter =>
                {
                    if (parameter is GuideInlineAction action)
                        SelectDiscoveryOption(action);
                });

            ToggleAutomaticRepairsCommand =
                new RelayCommand(
                    () => ToggleAutomaticRepairs(),
                    () => expeditionActive);
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

            if (awaitingDecisionChoice)
            {
                if (await TryApplyTypedDecisionChoice(answer))
                    return;

                Conversation.AddGuideMessage(
                    "Please choose one of the options shown above, or type its exact label.");

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
        public async void SelectDiscoveryOption(
            GuideInlineAction action)
        {
            if (action == null)
                return;

            // The existing inline-action UI is generic. During a decision
            // sequence the same clickable transport carries an Expert decision
            // rather than a discovery choice.
            if (awaitingDecisionChoice)
            {
                await ApplyDecisionChoice(
                    action.ActionId);
                return;
            }

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
            DiscoveryOptions.Clear();
            NotifyScoutControlsChanged();

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
        /// Returns the Guide to the immediately preceding reversible decision
        /// point. The Guide restores only generic navigation state; the owning
        /// Expert restores its own domain state through the workflow boundary.
        /// </summary>
        private void GoBack()
        {
            // Expert decisions take precedence. The existing Expert-owned
            // rewind behavior remains unchanged.
            if (decisionHistory.Count > 0 &&
                awaitingDecisionChoice)
            {
                AppliedDecision previous =
                decisionHistory.Pop();

                guideInvestigator.RewindDecision(
                    previous.ExpertName);

                pendingDecisionBindings.Clear();
                pendingDecisionBindings.AddRange(
                    guideInvestigator.DecisionBindings
                        .Where(binding =>
                            binding.Request.Options.Count > 0));

                pendingDecisionIndex = 0;

                CanGoBack = expeditionActive;

                Conversation.AddGuideMessage(
                    "Let's go back to the previous decision so you can reconsider it.");

                if (pendingDecisionBindings.Count == 0)
                {
                    awaitingDecisionChoice = false;
                    ContinueInvestigationConversation();
                    return;
                }

                awaitingDecisionChoice = true;
                PresentDecisionQuestion();
                return;
            }

            // No Expert checkpoint remains. Back at this level means
            // abandon the current expedition and return to source-folder
            // selection. A fresh GuideInvestigator also guarantees that
            // selecting the same folder again starts a genuinely new
            // expedition rather than reusing Expert state from the old one.
            if (expeditionActive)
            {
                ResetExpedition();
            }
        }

        private void ResetExpedition()
        {
            guideInvestigator.Dispose();

            guideInvestigator =
                new GuideInvestigator(investigationProgress);

            guideInvestigator.BackgroundActionCompleted +=
                GuideInvestigator_BackgroundActionCompleted;

            currentWorkflow = null;
            selectedFolder = null;

            pendingDiscoveryBindings.Clear();
            pendingDiscoveryIndex = 0;
            awaitingDiscoveryChoice = false;

            pendingDecisionBindings.Clear();
            pendingDecisionIndex = 0;
            awaitingDecisionChoice = false;
            decisionHistory.Clear();

            ActionOptions.Clear();
            DecisionOptions.Clear();
            DiscoveryOptions.Clear();
            NotifyScoutControlsChanged();
            workspace.ConversationEngine.ClearActionOptions();

            expeditionActive = false;
            AutomaticRepairsEnabled = true;
            CanGoBack = false;
            stage = ConversationStage.Greeting;

            Conversation.Clear();
            Conversation.Messages.Add(
                new GuideMessage
                {
                    IsGuide = true,

                    Card = new FolderPickerCard
                    {
                        Command = BrowseFolderCommand
                    }
                });
        }

        private void RecordAppliedDecision(
            ExpertDecisionBinding binding)
        {
            if (guideInvestigator.CanRewindDecision(
                    binding.Expert.Name))
            {
                decisionHistory.Push(
                    new AppliedDecision
                    {
                        ExpertName = binding.Expert.Name
                    });
            }

            CanGoBack = expeditionActive;
        }

        /// <summary>
        /// Applies a generic Expert decision selected from the conversation.
        ///
        /// If the selected option requests a generic folder input, the Guide
        /// uses the existing folder picker and transports the selected path
        /// back through GuideInvestigator. The Guide never interprets the path
        /// as an Ebook destination; that meaning remains inside the Expert.
        /// </summary>
        private async Task ApplyDecisionChoice(
            string optionId)
        {
            if (!awaitingDecisionChoice)
                return;

            if (pendingDecisionIndex < 0 ||
                pendingDecisionIndex >= pendingDecisionBindings.Count)
            {
                awaitingDecisionChoice = false;
                return;
            }

            ExpertDecisionBinding binding =
                pendingDecisionBindings[pendingDecisionIndex];

            ExpertDecisionOption? option =
                binding.Request.Options.FirstOrDefault(
                    candidate =>
                        string.Equals(
                            candidate.Id,
                            optionId,
                            StringComparison.OrdinalIgnoreCase));

            if (option == null)
                return;

            string? value = null;

            if (option.InputKind == ExpertDecisionInputKind.Folder)
            {
                value = guideInvestigator.PickFolder();

                if (string.IsNullOrWhiteSpace(value))
                {
                    Conversation.AddGuideMessage(
                        "No destination was selected. The proposed destination is still waiting for your confirmation.");
                    return;
                }
            }

            operation.State = ScoutOperationState.Running;
            operation.Status = "Applying your decision...";

            await guideInvestigator.ApplyDecisionChoiceAsync(
                binding.Expert.Name,
                option.Id,
                value);

            RecordAppliedDecision(binding);

            Conversation.AddUserMessage(
                option.InputKind == ExpertDecisionInputKind.Folder
                    ? value!
                    : option.Label);

            AdvanceDecisionSequence();

            if (awaitingDecisionChoice)
            {
                operation.State = ScoutOperationState.WaitingForUser;
                operation.Status = "Waiting for your decision.";
            }
            else
            {
                operation.State = ScoutOperationState.Completed;
                operation.Status = "Decision complete.";
            }
        }

        /// <summary>
        /// Accepts a typed decision answer. Matching is deliberately limited
        /// to the options supplied by the current Expert. For folder-input
        /// options, a typed path is also accepted when it names an existing
        /// directory.
        /// </summary>
        private async Task<bool> TryApplyTypedDecisionChoice(
            string answer)
        {
            if (!awaitingDecisionChoice ||
                pendingDecisionIndex < 0 ||
                pendingDecisionIndex >= pendingDecisionBindings.Count)
            {
                return false;
            }

            ExpertDecisionBinding binding =
                pendingDecisionBindings[pendingDecisionIndex];

            ExpertDecisionOption? option =
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

            if (option != null)
            {
                if (option.InputKind == ExpertDecisionInputKind.Folder)
                {
                    string? folder =
                        guideInvestigator.PickFolder();

                    if (string.IsNullOrWhiteSpace(folder))
                        return true;

                    operation.State = ScoutOperationState.Running;
                    operation.Status = "Applying your decision...";

                    await guideInvestigator.ApplyDecisionChoiceAsync(
                        binding.Expert.Name,
                        option.Id,
                        folder);

                    Conversation.AddUserMessage(folder);
                }
                else
                {
                    operation.State = ScoutOperationState.Running;
                    operation.Status = "Applying your decision...";

                    await guideInvestigator.ApplyDecisionChoiceAsync(
                        binding.Expert.Name,
                        option.Id);

                    Conversation.AddUserMessage(option.Label);
                }

                RecordAppliedDecision(binding);

                AdvanceDecisionSequence();
                operation.State = awaitingDecisionChoice
                    ? ScoutOperationState.WaitingForUser
                    : ScoutOperationState.Completed;
                operation.Status = awaitingDecisionChoice
                    ? "Waiting for your decision."
                    : "Decision complete.";
                return true;
            }

            // A folder path can be supplied directly when the current Expert
            // decision asks for a folder. This remains generic transport; the
            // owning Expert decides whether that value is valid for its domain.
            ExpertDecisionOption? folderOption =
                binding.Request.Options.FirstOrDefault(
                    candidate =>
                        candidate.InputKind == ExpertDecisionInputKind.Folder);

            if (folderOption != null &&
                Directory.Exists(answer))
            {
                operation.State = ScoutOperationState.Running;
                operation.Status = "Applying your decision...";

                await guideInvestigator.ApplyDecisionChoiceAsync(
                    binding.Expert.Name,
                    folderOption.Id,
                    Path.GetFullPath(answer));

                Conversation.AddUserMessage(
                    Path.GetFullPath(answer));

                RecordAppliedDecision(binding);

                AdvanceDecisionSequence();
                operation.State = awaitingDecisionChoice
                    ? ScoutOperationState.WaitingForUser
                    : ScoutOperationState.Completed;
                operation.Status = awaitingDecisionChoice
                    ? "Waiting for your decision."
                    : "Decision complete.";
                return true;
            }

            return false;
        }

        /// <summary>
        /// Presents the current generic Expert decision without interpreting
        /// the question or its options.
        /// </summary>
        private void PresentDecisionQuestion()
        {
            if (pendingDecisionIndex < 0 ||
                pendingDecisionIndex >= pendingDecisionBindings.Count)
            {
                awaitingDecisionChoice = false;
                return;
            }

            ExpertDecisionBinding binding =
                pendingDecisionBindings[pendingDecisionIndex];

            DecisionOptions.Clear();

            Conversation.AddGuideMessage(
                binding.Request.Question);

            foreach (ExpertDecisionOption option
                in binding.Request.Options)
            {
                DecisionOptions.Add(
                    new GuideInlineAction(
                        option.Label,
                        option.Id));
            }

            NotifyScoutControlsChanged();
        }

        /// <summary>
        /// Advances to the next Expert decision or, when all decisions are
        /// complete, returns control to the normal recommendation conversation.
        /// </summary>
        private void AdvanceDecisionSequence()
        {
            pendingDecisionIndex++;

            if (pendingDecisionIndex < pendingDecisionBindings.Count)
            {
                PresentDecisionQuestion();
                return;
            }

            // The owning Expert may expose a new decision after applying the
            // previous one. Refresh the bindings before leaving the decision
            // sequence so a destination confirmation can naturally lead into
            // the organization-path decision.
            pendingDecisionBindings.Clear();
            pendingDecisionBindings.AddRange(
                guideInvestigator.DecisionBindings
                    .Where(binding =>
                        binding.Request.Options.Count > 0));

            if (pendingDecisionBindings.Count > 0)
            {
                pendingDecisionIndex = 0;
                PresentDecisionQuestion();
                return;
            }

            awaitingDecisionChoice = false;
            pendingDecisionIndex = 0;
            DecisionOptions.Clear();
            NotifyScoutControlsChanged();
            CanGoBack = expeditionActive;

            // Organization is a terminal collection-level stage for the
            // current expedition. Do not feed the old investigation
            // recommendation set back into the conversation after the
            // organization decision has already executed. Those findings
            // describe the state before the decision and otherwise appear
            // as an unrelated final message.
            if (string.Equals(
                    operation.Stage,
                    "Organization",
                    StringComparison.OrdinalIgnoreCase))
            {
                workspace.ConversationEngine.ClearActionOptions();
                ActionOptions.Clear();
                DecisionOptions.Clear();
                DiscoveryOptions.Clear();
                NotifyScoutControlsChanged();

                if (operation.CollectionTotal > 0 &&
                    operation.CollectionCompleted >= operation.CollectionTotal)
                {
                    // The Expert has reached the terminal collection outcome.
                    // Reconcile the generic Live Report rows with that domain
                    // result instead of leaving previously-processed books in
                    // WORKING because their last progress snapshot said
                    // Processing/Waiting.
                    operation.MarkAllItemsCompleted();
                    operation.State = ScoutOperationState.Completed;
                    operation.Status = "Organization complete.";
                    operation.CurrentTask =
                        $"{operation.CollectionCompleted:N0}/{operation.CollectionTotal:N0} organized";

                    Conversation.AddGuideMessage(
                        $"Organization is complete. Scout processed all {operation.CollectionTotal:N0} items.");
                }
                else
                {
                    operation.State =
                        operation.CollectionWaiting > 0
                            ? ScoutOperationState.WaitingForUser
                            : ScoutOperationState.Completed;

                    operation.Status =
                        operation.CollectionWaiting > 0
                            ? "Organization complete; some ebooks still need attention."
                            : "Organization finished with pending work.";

                    Conversation.AddGuideMessage(
                        "Organization has finished, but some ebooks still need attention before the collection is fully organized.");
                }

                return;
            }

            ContinueInvestigationConversation();
        }

        /// <summary>
        /// Starts the current generic Expert decision sequence.
        /// Returns true when a decision was presented and the normal
        /// recommendation conversation must wait.
        /// </summary>
        private bool BeginDecisionSequence()
        {
            decisionHistory.Clear();
            CanGoBack = expeditionActive;
            pendingDecisionBindings.Clear();
            pendingDecisionBindings.AddRange(
                guideInvestigator.DecisionBindings
                    .Where(binding =>
                        binding.Request.Options.Count > 0));

            pendingDecisionIndex = 0;

            if (pendingDecisionBindings.Count == 0)
            {
                awaitingDecisionChoice = false;
                return false;
            }

            awaitingDecisionChoice = true;
            PresentDecisionQuestion();
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

            DiscoveryOptions.Clear();

            Conversation.AddGuideMessage(
                "Before I investigate, I need one choice about how you'd like me to search.");

            foreach (ExpertDiscoveryOption option
                in binding.Request.Options)
            {
                DiscoveryOptions.Add(
                    new GuideInlineAction(
                        option.Label,
                        option.Id));
            }

            NotifyScoutControlsChanged();
        }

        /// <summary>
        /// Starts the generic discovery-choice sequence for the selected folder.
        /// If no Expert requires a choice, investigation begins immediately.
        /// </summary>
        private void BeginDiscovery(
            string folder)
        {
            expeditionActive = true;
            CanGoBack = true;
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
        private async void InvestigateSelectedFolder(
            string folder)
        {
            operation.Title = "Investigating Project";
            operation.Status = "Starting investigation...";
            operation.CurrentTask = "Preparing...";
            operation.CurrentFile = "";
            operation.CompletedSteps = 0;
            operation.TotalSteps = 0;
            operation.Stage = "";
            OnPropertyChanged(nameof(ShowWorkspaceObservations));
            operation.StageCompleted = 0;
            operation.StageTotal = 0;
            operation.CollectionTotal = 0;
            operation.CollectionCompleted = 0;
            operation.CollectionProcessing = 0;
            operation.CollectionWaiting = 0;
            operation.CollectionPending = 0;
            operation.ApplyItems(Array.Empty<ExecutionProgressItem>());
            
            operation.State = ScoutOperationState.Running;

            try
            {
                WorkflowResult? result =
                    await Task.Run(
                        () => guideInvestigator.Investigate(folder));

                if (result != null)
                {
                    operation.Status =
                        operation.CollectionWaiting > 0
                            ? "Investigation complete; waiting for your decision."
                            : "Investigation complete.";

                    operation.CurrentTask =
                        operation.CollectionTotal > 0
                            ? $"{operation.CollectionCompleted:N0}/{operation.CollectionTotal:N0} complete"
                            : "Ready for review.";

                    operation.State =
                        operation.CollectionWaiting > 0
                            ? ScoutOperationState.WaitingForUser
                            : ScoutOperationState.Completed;
                }

                CompleteInvestigation(result);
            }
            catch (Exception ex)
            {
                operation.Status = "Investigation failed.";
                operation.CurrentTask = ex.Message;
                operation.State = ScoutOperationState.Failed;

                Conversation.AddGuideMessage(
                    "I wasn't able to complete the investigation." +
                    Environment.NewLine +
                    ex.Message);
            }
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
            // Collection-level Expert decisions must be completed before the
            // normal recommendation conversation begins. Organization uses
            // this stage to confirm the destination and then choose its path.
            // -------------------------------------------------------------

            if (BeginDecisionSequence())
                return;

            ContinueInvestigationConversation();
        }

        /// <summary>
        /// Loads the investigation recommendations into the Workspace's
        /// authoritative Conversation Engine and begins the normal
        /// recommendation conversation. This is reached only after any
        /// collection-level Expert decisions have been completed.
        /// </summary>
        private void ContinueInvestigationConversation()
        {
            if (currentWorkflow == null)
                return;

            workspace.ConversationEngine.LoadRecommendations(
                currentWorkflow.ObservationRecommendations);

            CV_Recommendation? firstRecommendation =
                currentWorkflow.ObservationRecommendations
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
        private async void SelectDecisionOption(
            GuideInlineAction action)
        {
            if (action == null || !awaitingDecisionChoice)
                return;

            await ApplyDecisionChoice(action.ActionId);
        }

        /// <summary>
        /// Executes a collection-item action through the same CV_ActionRequest
        /// path used by conversation actions and the bottom action bar.
        /// </summary>
        private async void AcceptAllAsIs()
        {
            if (!operation.HasNeedsItems)
                return;

            MessageBoxResult confirmation =
                MessageBox.Show(
                    $"Accept all {operation.NeedsItems.Count():N0} items as-is?\n\n" +
                    "Scout will stop asking for missing or unresolved information " +
                    "for these books and continue toward Organization.",
                    "Accept all items as-is",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

            if (confirmation != MessageBoxResult.OK)
                return;

            CV_ActionRequest actionRequest = new()
            {
                ActionId = "AcceptAllAsIs",
                OptionId = "AcceptAllAsIs",
                ContextId = ""
            };

            operation.State = ScoutOperationState.Running;
            operation.Status = "Accepting books as-is...";

            CV_ActionResult actionResult =
                await ExecuteActionAsync(actionRequest);

            HandleActionResult(actionResult);
        }

        /// <summary>
        /// Executes a collection-item action through the same CV_ActionRequest
        /// path used by conversation actions and the bottom action bar.
        /// </summary>
        private async void ExecuteProgressAction(
            ExecutionProgressAction action)
        {
            if (action == null ||
                string.IsNullOrWhiteSpace(action.ActionId) ||
                string.IsNullOrWhiteSpace(action.ContextId))
            {
                return;
            }

            CV_ActionRequest actionRequest = new()
            {
                ActionId = action.ActionId,
                OptionId = action.Id,
                ContextId = action.ContextId
            };

            workspace.ConversationEngine.ClearActionOptions();
            ActionOptions.Clear();
            NotifyScoutControlsChanged();

            operation.State = ScoutOperationState.Running;
            operation.Status = "Working...";

            // Live Report actions are already visible in the report. Do not
            // echo every click into the scrolling conversation transcript.

            CV_ActionResult actionResult =
                await ExecuteActionAsync(actionRequest);

            HandleActionResult(actionResult);
        }

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
            NotifyScoutControlsChanged();

            operation.State = ScoutOperationState.Running;
            operation.Status = "Working...";

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

            operation.State = ScoutOperationState.Running;
            operation.Status = "Working...";

            CV_ActionResult actionResult =
                await ExecuteActionAsync(
                    actionRequest);

            HandleActionResult(actionResult);
        }

        /// <summary>
        /// Routes the visible automatic-repair switch through the same generic
        /// Conversation Framework action used by the conversation and
        /// recommendation hotlink.
        /// </summary>
        private async void ToggleAutomaticRepairs()
        {
            string actionId =
                AutomaticRepairsEnabled
                    ? "RevokeAutomaticAction"
                    : "AuthorizeAutomaticAction";

            CV_ActionRequest actionRequest = new()
            {
                ActionId = actionId,
                IsStandaloneAction = true
            };

            operation.State = ScoutOperationState.Running;
            operation.Status =
                AutomaticRepairsEnabled
                    ? "Enabling automatic repairs..."
                    : "Disabling automatic repairs...";

            CV_ActionResult actionResult =
                await ExecuteActionAsync(actionRequest);

            HandleActionResult(actionResult);
        }

        private void GuideInvestigator_BackgroundActionCompleted(
            object? sender,
            ExpertBackgroundActionCompletedEventArgs e)
        {
            if (!ReferenceEquals(sender, guideInvestigator))
                return;

            Action applyResult =
                () =>
                {
                    // Background research completes outside the original
                    // investigation pass. Refresh the affected Live Report
                    // row before presenting the result so a completed research
                    // item cannot remain visually stuck in WORKING.
                    operation.ApplyBackgroundActionResult(
                        e.ContextId,
                        e.Result);

                    HandleActionResult(e.Result);
                };

            if (System.Windows.Application.Current?.Dispatcher is
                System.Windows.Threading.Dispatcher dispatcher)
            {
                _ = dispatcher.InvokeAsync(applyResult);
            }
            else
            {
                applyResult();
            }
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

            if (string.Equals(
                    actionResult.ActionId,
                    "AuthorizeAutomaticAction",
                    StringComparison.OrdinalIgnoreCase))
            {
                AutomaticRepairsEnabled = actionResult.Success;
            }
            else if (string.Equals(
                    actionResult.ActionId,
                    "RevokeAutomaticAction",
                    StringComparison.OrdinalIgnoreCase) &&
                     actionResult.Success)
            {
                AutomaticRepairsEnabled = false;
            }

            // -------------------------------------------------------------
            // Report a failed action.
            // -------------------------------------------------------------

            if (!actionResult.Success)
            {
                if (string.Equals(
                        actionResult.ActionId,
                        "AuthorizeAutomaticAction",
                        StringComparison.OrdinalIgnoreCase))
                {
                    AutomaticRepairsEnabled = false;
                }
                else if (string.Equals(
                        actionResult.ActionId,
                        "RevokeAutomaticAction",
                        StringComparison.OrdinalIgnoreCase))
                {
                    AutomaticRepairsEnabled = true;
                }

                operation.State = ScoutOperationState.Failed;

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
            // Background research is work Scout is doing for the user, not a
            // request for a running transcript of every research fact. Keep
            // that detail attached to the action result and only narrate it
            // when the result actually leaves a user decision.

            bool isBackgroundResearch =
                actionResult.ActionId.StartsWith(
                    "BackgroundResearch",
                    StringComparison.OrdinalIgnoreCase);

            if (!isBackgroundResearch || actionResult.Options.Count > 0)
            {
                foreach (string evidence in actionResult.Evidence)
                {
                    if (!string.IsNullOrWhiteSpace(evidence))
                    {
                        Conversation.AddGuideMessage(
                            evidence);
                    }
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
                // An input prompt is a conversation state, not a button.
                // The Conversation Engine retains it so the next typed
                // message becomes the supplied value.
                if (option.AcceptsUserInput)
                    continue;

                // The persistent Scout Controls area is the sole visual
                // action surface for structured action options. Do not add
                // another clickable copy to the conversation transcript.
                ActionOptions.Add(option);
            }

            NotifyScoutControlsChanged();

            if (isBackgroundResearch && actionResult.Options.Count > 0)
            {
                Conversation.AddGuideMessage(
                    "I need your decision on this result. The available choices are shown in Scout Controls on the left.");
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
                operation.State =
                    operation.CollectionWaiting > 0
                        ? ScoutOperationState.WaitingForUser
                        : ScoutOperationState.Completed;

                IReadOnlyList<CV_Recommendation> recommendations =
                    guideInvestigator.ReobservationRecommendations;

                IReadOnlyList<ProjectObservation> observations =
                    guideInvestigator.ReobservationObservations;

                workspace.ConversationEngine.ClearActionOptions();
                ActionOptions.Clear();
                DecisionOptions.Clear();
                DiscoveryOptions.Clear();
                NotifyScoutControlsChanged();

                workspace.RefreshAfterReobservation(
                    observations,
                    recommendations);

                if (recommendations.Count > 0)
                {
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

                // A terminal repair decision may have removed the final
                // collection-wide repair blocker. Refresh the generic Expert
                // decision bindings now so Organization can become the next
                // explicit step instead of leaving the user at an apparently
                // complete 100% screen with no way forward.
                pendingDecisionBindings.Clear();
                pendingDecisionBindings.AddRange(
                    guideInvestigator.DecisionBindings
                        .Where(binding =>
                            binding.Request.Options.Count > 0));

                if (pendingDecisionBindings.Count > 0)
                {
                    pendingDecisionIndex = 0;
                    awaitingDecisionChoice = true;
                    operation.State =
                        ScoutOperationState.WaitingForUser;
                    operation.Status =
                        "Investigation complete; ready for the next collection decision.";
                    PresentDecisionQuestion();
                }
            }
            else
            {
                DecisionOptions.Clear();
                NotifyScoutControlsChanged();

                workspace.ClearCurrentRecommendation();

                // A successful action is not necessarily terminal. For
                // example, accepting an unresolved ebook as-is should return
                // it to WORKING so organization/re-observation can continue.
                // Derive the generic operation state from the Live Report
                // rows instead of assuming every successful action completes
                // the operation.
                if (operation.HasNeedsItems)
                {
                    operation.State = ScoutOperationState.WaitingForUser;
                    operation.Status = "Waiting for your decision.";
                }
                else if (operation.HasWorkingItems)
                {
                    operation.State = ScoutOperationState.Running;
                    operation.Status = "Working...";
                }
                else
                {
                    operation.State = ScoutOperationState.Completed;
                    operation.Status = "Complete.";
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
