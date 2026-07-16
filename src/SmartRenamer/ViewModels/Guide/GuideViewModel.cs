using System;
using System.Collections.Generic;
using SmartRenamer.Guide;
using SmartRenamer.Guide.Models;
using SmartRenamer.Guide.Thinking;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;

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

            Conversation.AddGuideMessage(
                "I'll help organize your project.");

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

            //-------------------------------------------------
            // Let Scout understand the answer.
            //-------------------------------------------------

            conversationEngine.ProcessAnswer(answer);

            //-------------------------------------------------
            // Conversation Flow
            //-------------------------------------------------

            switch (stage)
            {
                case ConversationStage.Greeting:

                    AskNextQuestion();

                    Conversation.AddGuideMessage("");

                    Conversation.AddGuideMessage(
                        "When you're ready, I'll investigate the folder.");

                    Conversation.AddGuideMessage(
                        "Just type Go.");

                    stage = ConversationStage.ChooseFolder;

                    break;

                case ConversationStage.ChooseFolder:

                    if (answer.Equals(
                        "go",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        Conversation.AddGuideMessage(
                            "Opening the folder browser...");

                        ChooseFolder();
                    }
                    else
                    {
                        AskNextQuestion();
                    }

                    break;

                case ConversationStage.ReviewPlan:

                    AskNextQuestion();

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

            ProjectCreated?.Invoke(this, result);

            Conversation.AddGuideMessage("");

            Conversation.AddGuideMessage(
                "I've finished looking through your folder.");

            Conversation.AddGuideMessage("");

            Conversation.AddGuideMessage(
                guideInvestigator.Summarize(result));

            Conversation.AddGuideMessage("");

            Conversation.AddGuideMessage(
                "Now I'd like to understand one more thing before I recommend anything.");

            stage = ConversationStage.ReviewPlan;

            AskNextQuestion();
        }
    }
}