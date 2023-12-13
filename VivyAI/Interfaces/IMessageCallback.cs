namespace VivyAI.Interfaces
{
    internal struct CallbackId
    {
        public string name;

        public CallbackId(string name)
        {
            this.name = name;
        }
    }

    internal struct CallbackCallId
    {
        public string userId;
        public string messageId;

        public CallbackCallId(string userId, string messageId) : this()
        {
            this.userId = userId;
            this.messageId = messageId;
        }
    }

    internal interface IMessageCallback
    {
        CallbackId Id { get; }
        int LockCode { get; }
        void Run(CallbackCallId id);
    }
}