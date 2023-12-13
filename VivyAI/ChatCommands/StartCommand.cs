using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class StartCommand : IChatCommand
    {
        string IChatCommand.CommandName => "start";
        bool IChatCommand.IsAdminCommand => false;
        public void Execute(IChat chat, IChatMessage message)
        {
            chat.Reset();
            App.SendAppMessage(chat.Id, new ChatMessage()
            {
                Content = Strings.Warning
            });
        }
    }
}