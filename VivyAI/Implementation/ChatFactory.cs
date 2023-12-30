using VivyAI.Interfaces;

namespace VivyAI.Implementation
{
    internal sealed class ChatFactory : IChatFactory
    {
        private readonly IAiAgentFactory aIAgentFactory;
        private readonly IMessenger messenger;

        public ChatFactory(IAiAgentFactory aIAgentFactory, IMessenger messenger)
        {
            this.aIAgentFactory = aIAgentFactory;
            this.messenger = messenger;
        }

        public IChat CreateChat(string chatId)
        {
            return new Chat(chatId, aIAgentFactory.CreateAiAgent(), messenger);
        }
    }
}