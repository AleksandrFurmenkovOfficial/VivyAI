using VivyAI.Interfaces;

namespace VivyAI.MessageCallbacks
{
    internal sealed class CancelCallback : IMessageCallback
    {
        public static string cName => "Cancel";
        public static CallbackId cId => new(cName);

        public CallbackId Id => cId;

        public int LockCode => IChat.cancelCode;

        private readonly Func<string, IChat> chatGetter;

        public CancelCallback(Func<string, IChat> chatGetter)
        {
            this.chatGetter = chatGetter;
        }

        public void Run(CallbackCallId id)
        {
            chatGetter(id.userId).Cancel();
        }
    }
}