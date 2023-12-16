namespace VivyAI.Interfaces
{
    internal interface IChatCommandProcessor
    {
        bool ExecuteIfChatCommand(string chatId, IChatMessage message);
    }
}