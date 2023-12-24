using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class CommonCommand : IChatCommand
    {
        string IChatCommand.Name => "common";
        bool IChatCommand.IsAdminCommand => false;
        private readonly IMessenger Messenger;
        public CommonCommand(IMessenger Messenger)
        {
            this.Messenger = Messenger;
        }
        public async void Execute(IChat chat, IChatMessage message)
        {
            chat.SetCommonMode();
            _ = await Messenger.SendMessage(chat.Id, new ChatMessage()
            {
                Content = Strings.EnglishModeNow
            }).ConfigureAwait(false);
        }
    }
}