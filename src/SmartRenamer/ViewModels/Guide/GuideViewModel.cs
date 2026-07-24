using SmartRenamer.Controls.ConversationCards;
using SmartRenamer.Guide;
using SmartRenamer.Guide.Models;
using SmartRenamer.Guide.Thinking;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Rename;
using SmartRenamer.Services;
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

        private ConversationStage stage = ConversationStage.Greeting;

        public GuideConversation Conversation { get; } = new();

        public event EventHandler<WorkflowResult>? ProjectCreated;
        public event EventHandler? PlanApproved;

        private WorkflowResult? currentWorkflow;

        private string userInput = "";

        public string UserInput
        {
            get => userInput;
            set => SetProperty(ref userInput, value);
        }

        public RelayCommand SendCommand { get; }
        public RelayCommand BrowseFolderCommand { get; }

        public GuideViewModel()
        {
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
                    Conversation.AddGuideMessage("Opening the folder browser...");

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
            System.Diagnostics.Debug.WriteLine("ChooseFolder() called.");

            WorkflowResult? result = guideInvestigator.Investigate();

            if (result == null)
            {
                Conversation.AddGuideMessage("No folder was selected.");

                stage = ConversationStage.Greeting;

                return;
            }

            currentWorkflow = result;

            ProjectCreated?.Invoke(this, result);

            Conversation.AddGuideMessage(
                "I had a chance to explore your folder.");

            Conversation.AddGuideMessage(
                "Nothing has been changed. I was simply looking for patterns that might help us organize it better.");

            ProjectObservation? firstObservation =
                result.Project.Observations.FirstOrDefault();

            if (firstObservation != null)
            {
                Conversation.AddGuideMessage(
                    "Your files tell an interesting story.");

                Conversation.AddGuideMessage(
                    $"The first thing that caught my attention was {firstObservation.Title.ToLower()}.");

                Conversation.AddGuideMessage(
                    firstObservation.Description);
            }

            if (result.Project.Observations.Count > 1)
            {
                Conversation.AddGuideMessage(
                    $"There {(result.Project.Observations.Count == 2 ? "is" : "are")} also {result.Project.Observations.Count - 1} other discovery{(result.Project.Observations.Count - 1 == 1 ? "" : "ies")} I'd like to show you.");
            }

            Conversation.AddGuideMessage(
                $"Based on what I found, I prepared a safe preview with {result.Preview.Count} proposed change{(result.Preview.Count == 1 ? "" : "s")}.");

            Conversation.AddGuideMessage(
                "We'll be able to review every recommendation together before anything is changed.");

            Conversation.AddGuideMessage(
                "Select the first observation on the left and I'll explain why I think it's the best place to start.");

            // Stop here temporarily.
        }
    }
}