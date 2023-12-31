namespace VivyAI.Interfaces
{
    internal class ResponseStreamChunk
    {
        public readonly List<IChatMessage> Messages;

        public readonly string TextDelta;

        public ResponseStreamChunk(string textDelta) : this(null, textDelta)
        {
            TextDelta = textDelta;
        }

        public ResponseStreamChunk(IEnumerable<IChatMessage> messages, string textDelta = "")
        {
            TextDelta = textDelta;
            Messages = new List<IChatMessage>();

            if (messages != null)
            {
                Messages.AddRange(messages);
            }
        }
    }
}