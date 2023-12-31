using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatMessageActions
{
    internal sealed class RetryAction : RegenerateAction
    {
        public new static ActionId Id => new("Retry");

        public override ActionId GetId => Id;

        public override async Task Run(IChat chat)
        {
            await chat.RemoveResponse().ConfigureAwait(false);
            await base.Run(chat).ConfigureAwait(false);
        }
    }
}