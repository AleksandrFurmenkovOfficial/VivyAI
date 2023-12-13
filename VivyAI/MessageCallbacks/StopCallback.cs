using VivyAI.Interfaces;

namespace VivyAI.MessageCallbacks
{
    internal sealed class StopCallback : IMessageCallback
    {
        public static string cName => "Stop";
        public static CallbackId cId => new(cName);

        public CallbackId Id => cId;

        public int LockCode => IChat.stopCode;

        private readonly Func<string, IChat> chatGetter;

        public StopCallback(Func<string, IChat> chatGetter)
        {
            this.chatGetter = chatGetter;
        }

        public void Run(CallbackCallId id)
        {
            chatGetter(id.userId).Stop();
        }
    }
}