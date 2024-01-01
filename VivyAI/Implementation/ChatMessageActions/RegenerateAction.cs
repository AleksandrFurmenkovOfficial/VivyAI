using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatMessageActions
{
    internal class RegenerateAction : IChatMessageAction
    {
        public static ActionId Id => new("Regenerate");

        public virtual ActionId GetId => Id;

        public virtual Task Run(IChat chat)
        {
            return chat.RegenerateLastResponse();
        }
    }
}