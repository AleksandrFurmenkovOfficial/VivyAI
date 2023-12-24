using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class StartCommand : IChatCommand
    {
        string IChatCommand.Name => "start";
        bool IChatCommand.IsAdminCommand => false;
        private readonly IMessenger Messenger;
        public StartCommand(IMessenger Messenger)
        {
            this.Messenger = Messenger;
        }
        public async void Execute(IChat chat, IChatMessage message)
        {
            chat.Reset();
            _ = await Messenger.SendMessage(chat.Id, new ChatMessage()
            {
                Content = Strings.Warning
            }).ConfigureAwait(false);
        }
    }
}