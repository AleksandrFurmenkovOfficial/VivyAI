using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatCommands
{
    internal sealed class EnglishCommand : IChatCommand
    {
        string IChatCommand.Name => "english";
        bool IChatCommand.IsAdminOnlyCommand => false;

        public Task Execute(IChat chat, IChatMessage message)
        {
            chat.SetEnglishTeacherMode();
            return chat.SendSystemMessage(Strings.EnglishModeNow);
        }
    }
}