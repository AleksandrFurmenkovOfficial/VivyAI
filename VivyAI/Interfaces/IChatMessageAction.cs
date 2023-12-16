namespace VivyAI.Interfaces
{
    internal struct ActionId
    {
        public string name;

        public ActionId(string name)
        {
            this.name = name;
        }
    }

    internal struct ActionParameters
    {
        public string userId;
        public string messageId;

        public ActionParameters(string userId, string messageId) : this()
        {
            this.userId = userId;
            this.messageId = messageId;
        }
    }

    internal interface IChatMessageAction
    {
        ActionId GetId { get; }
        long LockCode { get; }
        void Run(ActionParameters id);
    }
}