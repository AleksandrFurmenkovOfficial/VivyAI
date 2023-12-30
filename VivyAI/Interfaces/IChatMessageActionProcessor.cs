namespace VivyAI.Interfaces
{
    internal interface IChatMessageActionProcessor
    {
        Task HandleMessageAction(IChat chat, ActionParameters actionCallParameters);
    }
}