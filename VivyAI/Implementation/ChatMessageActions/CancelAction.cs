using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatMessageActions
{
    internal sealed class CancelAction : IChatMessageAction
    {
        public static ActionId Id => new("Cancel");

        public ActionId GetId => Id;

        public Task Run(IChat chat, ActionParameters id)
        {
            return chat.RemoveResponse();
        }
    }
}