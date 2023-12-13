using VivyAI.Interfaces;

namespace VivyAI.MessageCallbacks
{
    internal sealed class RegenerateCallback : IMessageCallback
    {
        public static string cName => "Regenerate";
        public static CallbackId cId => new(cName);

        public CallbackId Id => cId;

        public int LockCode => IChat.noInterruptionCode;

        private readonly Func<string, IChat> chatGetter;

        public RegenerateCallback(Func<string, IChat> chatGetter)
        {
            this.chatGetter = chatGetter;
        }

        public void Run(CallbackCallId id)
        {
            chatGetter(id.userId).Regenerate(id.messageId);
        }
    }
}