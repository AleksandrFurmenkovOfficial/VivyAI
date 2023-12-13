namespace VivyAI.Interfaces
{
    internal interface IChatCommandProcessor
    {
        public bool ExecuteIfChatCommand(string chatId, IChatMessage message);
    }
}