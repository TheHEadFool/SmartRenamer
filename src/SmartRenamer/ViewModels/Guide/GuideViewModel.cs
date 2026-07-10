using SmartRenamer.Guide;
using SmartRenamer.Guide.Models;
using SmartRenamer.Infrastructure;

namespace SmartRenamer.ViewModels.Guide
{
    public class GuideViewModel : ObservableObject
    {
        private readonly GuideEngine guideEngine = new();

        private readonly GuideInvestigator guideInvestigator = new();

        private ConversationStage stage = ConversationStage.WaitingForGoal;

        public GuideConversation Conversation { get; } = new();

        private string userInput = "";

        public string UserInput
        {
            get => userInput;
            set => SetProperty(ref userInput, value);
        }

        public RelayCommand SendCommand { get; }

        public RelayCommand ChooseFolderCommand { get; }

        public GuideViewModel()
        {
            SendCommand = new RelayCommand(Send);

            ChooseFolderCommand = new RelayCommand(ChooseFolder);

            Conversation.AddGuideMessage("Welcome back!");

            Conversation.AddGuideMessage(
                "What would you like to work on today?");
        }

        private void Send()
        {
            if (string.IsNullOrWhiteSpace(UserInput))
                return;

            Conversation.AddUserMessage(UserInput);

            switch (stage)
            {
                case ConversationStage.WaitingForGoal:

                    guideEngine.RecordUserResponse(UserInput);

                    Conversation.AddGuideMessage(
                        "Great! I'll take a quick look before we decide what to do.");

                    Conversation.AddMessage(new GuideMessage
                    {
                        IsGuide = true,
                        MessageType = GuideMessageType.FolderPicker
                    });

                    stage = ConversationStage.WaitingForFolder;

                    break;

                case ConversationStage.WaitingForFolder:

                    Conversation.AddGuideMessage(
                        "Choose the folder using the card above.");

                    break;
            }

            UserInput = "";
        }

        private void ChooseFolder()
        {
            var project = guideInvestigator.Investigate();

            if (project == null)
            {
                Conversation.AddGuideMessage(
                    "No folder was selected.");

                return;
            }

            Conversation.AddGuideMessage(
                guideInvestigator.Summarize(project));

            Conversation.AddGuideMessage(
                "What would you like to organize first?");
        }
    }
}