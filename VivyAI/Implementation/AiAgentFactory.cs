using Rystem.OpenAi;
using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal sealed class AiAgentFactory(
        string openAiApiKey,
        IAiImagePainter aiImagePainter,
        IAiImageDescriptor aiGetImageDescription) : IAiAgentFactory
    {
        private static readonly SemaphoreSlim messagesLock = new(1, 1);

        public IAiAgent CreateAiAgent()
        {
            messagesLock.Wait();
            try
            {
                _ = OpenAiService.Instance.AddOpenAi(settings => { settings.ApiKey = openAiApiKey; }, "NoDi");
                var openAiApi = OpenAiService.Factory.Create("NoDi");
                return new OpenAiAgent(openAiApi, aiImagePainter, aiGetImageDescription);
            }
            finally
            {
                messagesLock.Release();
            }
        }
    }
}