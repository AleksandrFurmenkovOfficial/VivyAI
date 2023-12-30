namespace VivyAI.Interfaces
{
    internal interface IChatCommandProcessor
    {
        Task<bool> ExecuteIfChatCommand(IChat chat, IChatMessage message);
    }
}