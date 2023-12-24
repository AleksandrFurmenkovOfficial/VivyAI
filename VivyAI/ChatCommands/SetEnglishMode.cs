using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class EnglishCommand : IChatCommand
    {
        string IChatCommand.Name => "english";
        bool IChatCommand.IsAdminCommand => false;
        private readonly IMessenger Messenger;
        public EnglishCommand(IMessenger Messenger)
        {
            this.Messenger = Messenger;
        }
        public async void Execute(IChat chat, IChatMessage message)
        {
            chat.SetEnglishTeacherMode();
            _ = await Messenger.SendMessage(chat.Id, new ChatMessage()
            {
                Content = Strings.EnglishModeNow
            }).ConfigureAwait(false);
        }
    }
}