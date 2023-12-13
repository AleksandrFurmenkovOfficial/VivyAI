using VivyAI.Interfaces;

namespace VivyAI.MessageCallbacks
{
    internal sealed class ContinueCallback : IMessageCallback
    {
        public static string cName => "Continue";
        public static CallbackId cId => new(cName);

        public CallbackId Id => cId;

        public int LockCode => IChat.noInterruptionCode;

        private readonly Func<string, IChat> chatGetter;

        public ContinueCallback(Func<string, IChat> chatGetter)
        {
            this.chatGetter = chatGetter;
        }

        public void Run(CallbackCallId id)
        {
            chatGetter(id.userId).Continue();
        }
    }
}