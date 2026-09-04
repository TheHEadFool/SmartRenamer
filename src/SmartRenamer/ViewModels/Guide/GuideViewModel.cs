using Scout.Observations.Conversation;
using SmartRenamer.Controls.ConversationCards;
using SmartRenamer.Guide;
using SmartRenamer.Guide.Models;
using SmartRenamer.Guide.Thinking;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Rename;
using SmartRenamer.Services;
using SmartRenamer.ViewModels.Workspace;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        private void Send()
        {

            if (string.IsNullOrWhiteSpace(UserInput))
                return;

            string answer =
                UserInput.Trim();

            Conversation.AddUserMessage(
                answer);

            UserInput = "";

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
                        guideInvestigator.ExecuteAction(
                            actionRequest);

                    //---------------------------------------------------------
                    // Report a failed action.
                    //---------------------------------------------------------

                    HandleActionResult(actionResult);
                    return;

                    //---------------------------------------------------------
                    // Report the result returned by the domain Expert.
                    //---------------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(actionResult.Message))
                    {
                        Conversation.AddGuideMessage(
                            actionResult.Message);
                    }

                    //---------------------------------------------------------
                    // Report supporting evidence.
                    //---------------------------------------------------------

                    foreach (string evidence in actionResult.Evidence)
                    {
                        if (!string.IsNullOrWhiteSpace(evidence))
                        {
                            Conversation.AddGuideMessage(
                                evidence);
                        }
                    }

                    //---------------------------------------------------------
                    // Present structured options.
                    //
                    // For ISBN research these are the ISBN candidates returned
                    // by the Ebook Expert.
                    //---------------------------------------------------------

                    if (actionResult.Options.Count > 0)
                    {
                        workspace.ConversationEngine.RememberActionOptions(
                            actionResult.Options);

                        workspace.ConversationEngine.RememberActionOptions(
    actionResult.Options);

                        ActionOptions.Clear();

                        foreach (CV_ActionOption option
                            in actionResult.Options)
                        {
                            ActionOptions.Add(option);
                        }

                        Conversation.AddGuideMessage(
                            "Here are the results I found:");

                        foreach (CV_ActionOption nextOption
                            in actionResult.Options)
                        {
                            Conversation.Messages.Add(
                                new GuideMessage
                                {
                                    Speaker = GuideSpeaker.Guide,
                                    DisplayName = "Scout",
                                    Text = string.Empty,
                                    Payload = nextOption
                                });
                        }

                        Conversation.AddGuideMessage(
                            "You can choose one of these results or continue talking to me.");
                    }

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

        /// <summary>
        /// Executes a conversation action option selected by clicking
        /// the option displayed in the conversation.
        ///
        /// Clicking is simply another way of expressing the user's choice.
        /// It uses the same Conversation Framework action path as typed input.
        /// </summary>
        public void SelectActionOption(
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
    guideInvestigator.ExecuteAction(
        actionRequest);

            HandleActionResult(actionResult);
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
                ActionOptions.Add(option);
            }
        }

        // =====================================================================
        // Choose Folder
        // =====================================================================

        private void ChooseFolder()
        {
            System.Diagnostics.Debug.WriteLine(
                "ChooseFolder() called.");

            WorkflowResult? result =
                guideInvestigator.Investigate();

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

            //---------------------------------------------------------
            // The investigation is complete.
            //
            // Scout is now discussing the findings rather than
            // starting another folder-selection conversation.
            //---------------------------------------------------------

            stage =
                ConversationStage.InvestigationConversation;

            ProjectCreated?.Invoke(
                this,
                result);

            //---------------------------------------------------------
            // Initial investigation conversation
            //---------------------------------------------------------

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
        }
    }
}