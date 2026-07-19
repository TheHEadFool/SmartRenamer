using SmartRenamer.Guide;
using SmartRenamer.Guide.Models;
using SmartRenamer.Guide.Thinking;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Rename;
using SmartRenamer.Services;
using System;
using System.Collections.Generic;

namespace SmartRenamer.ViewModels.Guide
{
    public class GuideViewModel : ObservableObject
    {
        private readonly GuideInvestigator guideInvestigator = new();

        private readonly ScoutThoughtBuilder thoughtBuilder = new();

        private readonly ScoutConversationEngine conversationEngine = new();

        private ConversationStage stage =
            ConversationStage.Greeting;

        public GuideConversation Conversation { get; } = new();

        public event EventHandler<WorkflowResult>? ProjectCreated;

        // Raised when the user approves Scout's proposed plan.
        public event EventHandler? PlanApproved;

        // Remembers the most recent workflow so Scout
        // can execute it after the user approves.
        private WorkflowResult? currentWorkflow;

        private string userInput = "";

        public string UserInput
        {
            get => userInput;
            set => SetProperty(ref userInput, value);
        }

        public RelayCommand SendCommand { get; }

        public GuideViewModel()
        {
            SendCommand = new RelayCommand(Send);

            Conversation.AddGuideMessage("Hi, I'm Scout.");
            Conversation.AddGuideMessage("I'll help organize your project.");

            AskNextQuestion();
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

            conversationEngine.ProcessAnswer(answer);

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

                            PlanApproved?.Invoke(this, EventArgs.Empty);

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
            WorkflowResult? result =
                guideInvestigator.Investigate();

            if (result == null)
            {
                Conversation.AddGuideMessage(
                    "No folder was selected.");

                stage = ConversationStage.Greeting;

                return;
            }

            // Save the workflow.
            currentWorkflow = result;

            // Send it to the Workspace.
            ProjectCreated?.Invoke(this, result);

            Conversation.AddGuideMessage(
                $"DEBUG: Observations = {result.Project.Observations.Count}");

            Conversation.AddGuideMessage(
                $"DEBUG: Preview Items = {result.Preview.Count}");

            Conversation.AddGuideMessage(
                $"DEBUG: Recommendations loaded.");

            // Stop here temporarily.
        }
    }
}

