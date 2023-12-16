using VivyAI.Interfaces;

namespace VivyAI.MessageActions
{
    internal abstract class MessageActionsBase : IChatMessageAction
    {
        private readonly Func<string, IChat> chatGetter;

        public virtual ActionId GetId => throw new NotImplementedException();

        public virtual long LockCode => throw new NotImplementedException();

        public MessageActionsBase(Func<string, IChat> chatGetter)
        {
            this.chatGetter = chatGetter;
        }

        protected IChat GetChat(ActionParameters id)
        {
            return chatGetter(id.userId);
        }

        public virtual void Run(ActionParameters id)
        {
            // Proper LockCode is enough
        }
    }
}