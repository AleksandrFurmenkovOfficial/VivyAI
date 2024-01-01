namespace VivyAi.Interfaces
{
    internal interface IAiAgent : IAiImagePainter, IAiImageDescriptor
    {
        string AiName { set; get; }
        string SystemMessage { set; get; }
        bool EnableFunctions { set; get; }

        Task GetResponse(string chatId, IEnumerable<IChatMessage> messages,
            Func<ResponseStreamChunk, Task<bool>> responseStreamChunkGetter);

        Task<string> GetResponse(string setting, string question, string data = "");
    }
}