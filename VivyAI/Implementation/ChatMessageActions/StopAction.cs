using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatMessageActions
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