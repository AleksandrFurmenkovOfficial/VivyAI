using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatMessageActions
{
    internal sealed class ContinueAction : IChatMessageAction
    {
        public static ActionId Id => new("Continue");

        public ActionId GetId => Id;

        public Task Run(IChat chat)
        {
            return chat.ContinueLastResponse();
        }
    }
}