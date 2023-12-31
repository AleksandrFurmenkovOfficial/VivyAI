using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatMessageActions
{
    internal sealed class StopAction : IChatMessageAction
    {
        public static ActionId Id => new("Stop");

        public ActionId GetId => Id;

        public Task Run(IChat chat)
        {
            return Task.CompletedTask;
        }
    }
}