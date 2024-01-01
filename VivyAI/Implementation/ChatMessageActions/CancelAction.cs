using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatMessageActions
{
    internal sealed class CancelAction : IChatMessageAction
    {
        public static ActionId Id => new("Cancel");

        public ActionId GetId => Id;

        public Task Run(IChat chat)
        {
            return chat.RemoveResponse();
        }
    }
}