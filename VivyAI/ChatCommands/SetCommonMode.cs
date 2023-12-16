using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class CommonCommand : IChatCommand
    {
        string IChatCommand.Name => "common";
        bool IChatCommand.IsAdminCommand => false;
        public void Execute(IChat chat, IChatMessage message)
        {
            chat.SetCommonMode();
            App.SendAppMessage(chat.Id, new ChatMessage()
            {
                Content = Strings.CommonModeNow
            });
        }
    }
}