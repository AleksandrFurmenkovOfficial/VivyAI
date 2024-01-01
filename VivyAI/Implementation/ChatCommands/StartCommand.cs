using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatCommands
{
    internal sealed class StartCommand : IChatCommand
    {
        string IChatCommand.Name => "start";
        bool IChatCommand.IsAdminOnlyCommand => false;

        public Task Execute(IChat chat, IChatMessage message)
        {
            chat.Reset();
            return chat.SendSystemMessage(Strings.StartWarning);
        }
    }
}