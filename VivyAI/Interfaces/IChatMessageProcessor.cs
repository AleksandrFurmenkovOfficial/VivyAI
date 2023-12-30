namespace VivyAI.Interfaces
{
    internal interface IChatMessageProcessor
    {
        Task HandleMessage(IChat chat, IChatMessage message);
    }
}