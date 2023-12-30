namespace VivyAI.Interfaces
{
    internal readonly struct ActionParameters
    {
        public readonly string MessageId;
        public readonly ActionId ActionId;

        public ActionParameters(ActionId actionId, string messageId) : this()
        {
            ActionId = actionId;
            MessageId = messageId;
        }
    }
}