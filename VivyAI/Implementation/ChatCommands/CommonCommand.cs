using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatCommands
{
    internal sealed class CommonCommand : IChatCommand
    {
        string IChatCommand.Name => "common";
        bool IChatCommand.IsAdminOnlyCommand => false;

        public Task Execute(IChat chat, IChatMessage message)
        {
            chat.SetCommonMode();
            return chat.SendSystemMessage(Strings.CommonModeNow);
        }
    }
}