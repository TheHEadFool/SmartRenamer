using SmartRenamer.Guide;
using SmartRenamer.Guide.Models;
using SmartRenamer.Infrastructure;
using SmartRenamer.Services;

namespace SmartRenamer.ViewModels.Guide
{
    public class GuideViewModel : ObservableObject
    {
        private readonly GuideEngine guideEngine = new();

        private readonly FolderPicker folderPicker = new();

        private readonly FolderScanner folderScanner = new();

        private ConversationStage stage = ConversationStage.WaitingForGoal;

        public GuideConversation Conversation { get; } = new();

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

            Conversation.AddGuideMessage("Welcome back!");

            Conversation.AddGuideMessage("What would you like to do today?");
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
                        "Great! Show me the folder you're talking about.");

                    stage = ConversationStage.WaitingForFolder;

                    break;

                case ConversationStage.WaitingForFolder:

                    Conversation.AddGuideMessage(
                        "We'll connect this to the folder picker next.");

                    break;
            }

            UserInput = "";
        }
    }
}