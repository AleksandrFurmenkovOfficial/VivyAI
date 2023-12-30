using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatMessageActions
{
    internal sealed class RetryAction : RegenerateAction
    {
        public new static ActionId Id => new("Retry");

        public override ActionId GetId => Id;

        public override async Task Run(IChat chat, ActionParameters id)
        {
            await chat.RemoveResponse().ConfigureAwait(false);
            await base.Run(chat, id).ConfigureAwait(false);
        }
    }
}