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
using System.Linq;

namespace SmartRenamer.ViewModels.Guide
{
    public class GuideViewModel : ObservableObject
    {
        private readonly GuideInvestigator guideInvestigator = new();
        private readonly ScoutThoughtBuilder thoughtBuilder = new();
        private readonly ScoutConversationEngine conversationEngine = new();
        private readonly ProjectWorkspaceViewModel workspace;
       
        private ConversationStage stage = ConversationStage.Greeting;

        public GuideConversation Conversation { get; } = new();

        public event EventHandler<WorkflowResult>? ProjectCreated;
        public event EventHandler? PlanApproved;
        public event EventHandler? ReviewAllRequested;

        private WorkflowResult? currentWorkflow;

        private string userInput = "";

        public string UserInput
        {
            get => userInput;
            set => SetProperty(ref userInput, value);
        }

        public RelayCommand SendCommand { get; }
        public RelayCommand BrowseFolderCommand { get; }

        public GuideViewModel(ProjectWorkspaceViewModel workspace)
        {
            this.workspace = workspace;

            workspace.ConversationMessageGenerated +=
                Workspace_ConversationMessageGenerated;

            SendCommand = new RelayCommand(Send);

            BrowseFolderCommand = new RelayCommand(ChooseFolder);

            Conversation.Messages.Add(new GuideMessage
            {
                IsGuide = true,
                Card = new FolderPickerCard
                {
                    Command = BrowseFolderCommand
                }
            });
        }

        private void Workspace_ConversationMessageGenerated(
            object? sender,
            CV_ConversationMessage message)
        {
            if (message == null)
                return;

            if (string.IsNullOrWhiteSpace(message.Text))
                return;

            Conversation.AddGuideMessage(message.Text);
        }

        private void AskNextQuestion()
        {
            List<ScoutThought> thoughts =
                thoughtBuilder.Build(new ProjectContext());

            ScoutQuestion? question =
                conversationEngine.GetNextQuestion(thoughts);

            if (question != null)
            {
                Conversation.AddGuideMessage(question.Text);
            }
        }

        private void Send()
        {
            if (string.IsNullOrWhiteSpace(UserInput))
                return;

            string answer = UserInput.Trim();

            Conversation.AddUserMessage(answer);

            UserInput = "";

            //---------------------------------------------------------
            // NEW CONVERSATION FRAMEWORK
            //
            // Investigation conversations and Review All are now handled
            // by the Workspace's authoritative Conversation Engine.
            //
            // The legacy ScoutConversationEngine remains temporarily for
            // the older rename workflow below.
            //---------------------------------------------------------

            if (stage == ConversationStage.InvestigationConversation)
            {
                CV_UserIntent userIntent =
                    workspace.ConversationEngine.InterpretUserInput(answer);

                CV_ActionRequest? actionRequest =
    workspace.ConversationEngine.CreateActionRequest(answer);

                if (actionRequest != null)
                {
                    Conversation.AddGuideMessage("");

                    Conversation.AddGuideMessage(
                        $"I understand. You want me to proceed with " +
                        $"'{actionRequest.ActionId}'.");

                    return;
                }


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
                            "The research request has been recognized, but the research service is not connected yet.");

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

            //---------------------------------------------------------
            // LEGACY CONVERSATION FRAMEWORK
            //
            // This remains temporarily for the older rename workflow.
            // We will remove it after the new Conversation Framework
            // has absorbed those remaining responsibilities.
            //---------------------------------------------------------

            conversationEngine.ProcessAnswer(answer);

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

                    // This stage is no longer used because Scout
                    // automatically opens the folder browser.

                    break;

                case ConversationStage.InvestigationConversation:

                    //---------------------------------------------------------
                    // The Workspace owns the authoritative Conversation Engine.
                    //---------------------------------------------------------

                    CV_UserIntent userIntent =
                        workspace.ConversationEngine.InterpretUserInput(answer);

                    switch (userIntent.Type)
                    {
                        case CV_UserIntentType.Approve:

                            Conversation.AddGuideMessage(
                                "I understand. You want me to proceed with this recommendation.");

                            break;

                        case CV_UserIntentType.Research:

                            Conversation.AddGuideMessage(
                                "I understand. You want me to research the missing information.");

                            Conversation.AddGuideMessage(
                                "The research request has been recognized, but the research service is not connected yet.");

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

                    break;

                case ConversationStage.ReviewPlan:

                    switch (conversationEngine.GetIntent(answer))
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

            currentWorkflow = result;

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
                result.Project.Observations.FirstOrDefault();

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