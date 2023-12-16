namespace VivyAI.Interfaces
{
    internal sealed class ResponseStreamChunk
    {
        public ResponseStreamChunk() { isEnd = true; }
        public ResponseStreamChunk(string textStep) { this.textStep = textStep; }
        public ResponseStreamChunk(IChatMessage message, string textStep = "", bool isEnd = false)
        {
            this.textStep = textStep;
            this.isEnd = isEnd;
            messages.Add(message);
        }

        public readonly string textStep = "";
        public readonly List<IChatMessage> messages = new();
        public readonly bool isEnd;
    }

    internal interface IAIAgent
    {
        string AIName { set; get; }
        string SystemMessage { set; get; }
        bool EnableFunctions { set; get; }

        Task GetAIResponse(List<IChatMessage> messages, Func<ResponseStreamChunk, Task<bool>> responseStreamChunkGetter);

        Task<string> GetSingleResponse(string setting, string question, string data = "");

        Task<Uri> GetImage(string request, string userId);

        Task<string> GetImageDescription(Uri image, string question);
    }
}