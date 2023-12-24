using VivyAI.Interfaces;

namespace VivyAI.MessageActions
{
    internal sealed class RetryAction : RegenerateAction
    {
        public static new string Name => "Retry";
        public static new ActionId Id => new(Name);

        override public ActionId GetId => Id;

        override public long LockCode => IChat.noInterruptionCode;

        private readonly IMessenger Messenger;

        public RetryAction(Func<string, IChat> chatGetter, IMessenger Messenger) : base(chatGetter) { this.Messenger = Messenger; }

        public override async void Run(ActionParameters id)
        {
            var chat = GetChat(id);
            await Messenger.DeleteMessage(chat.Id, id.messageId).ConfigureAwait(false);
            base.Run(id);
        }
    }
}