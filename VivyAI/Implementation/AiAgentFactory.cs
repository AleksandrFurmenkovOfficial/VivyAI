using VivyAI.Interfaces;

namespace VivyAI.Implementation
{
    internal sealed class AiAgentFactory : IAiAgentFactory
    {
        private readonly string openAiApiKey;

        public AiAgentFactory(string openAiApiKey)
        {
            this.openAiApiKey = openAiApiKey;
        }

        public IAiAgent CreateAiAgent()
        {
            return new OpenAiAgent(openAiApiKey);
        }
    }
}