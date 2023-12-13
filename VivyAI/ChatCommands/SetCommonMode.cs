using VivyAI.Interfaces;

namespace VivyAI.Commands
{
    internal sealed class CommonCommand : IChatCommand
    {
        string IChatCommand.CommandName => "common";
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