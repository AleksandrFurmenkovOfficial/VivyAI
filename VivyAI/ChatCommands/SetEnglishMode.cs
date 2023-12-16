using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class EnglishCommand : IChatCommand
    {
        string IChatCommand.Name => "english";
        bool IChatCommand.IsAdminCommand => false;
        public void Execute(IChat chat, IChatMessage message)
        {
            chat.SetEnglishTeacherMode();
            App.SendAppMessage(chat.Id, new ChatMessage()
            {
                Content = Strings.EnglishModeNow
            });
        }
    }
}