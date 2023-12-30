namespace VivyAI.Interfaces
{
    internal sealed class ResponseStreamChunk
    {
        public readonly bool IsEnd;
        public readonly List<IChatMessage> Messages;
        
        public readonly string TextStep;

        public ResponseStreamChunk() : this(null, "", true)
        {
        }

        public ResponseStreamChunk(string textStep) : this(null, textStep, false)
        {
            TextStep = textStep;
        }

        public ResponseStreamChunk(IEnumerable<IChatMessage> messages, string textStep = "", bool isEnd = false)
        {
            TextStep = textStep;
            IsEnd = isEnd;
            Messages = new List<IChatMessage>();

            if (messages != null)
                Messages.AddRange(messages);
        }
    }
}