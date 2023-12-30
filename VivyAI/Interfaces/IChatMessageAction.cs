namespace VivyAI.Interfaces
{
    internal interface IChatMessageAction
    {
        ActionId GetId { get; }
        Task Run(IChat chat, ActionParameters id);
    }
}