namespace VivyAI.Interfaces
{
    internal interface IAiAgent
    {
        string AiName { set; get; }
        string SystemMessage { set; get; }
        bool EnableFunctions { set; get; }

        Task GetAiResponse(string chatId, IEnumerable<IChatMessage> messages,
            Func<ResponseStreamChunk, Task<bool>> responseStreamChunkGetter);

        Task<string> GetSingleResponse(string setting, string question, string data = "");

        Task<Uri> GetImage(string request, string userId);

        Task<string> GetImageDescription(Uri image, string question);
    }
}