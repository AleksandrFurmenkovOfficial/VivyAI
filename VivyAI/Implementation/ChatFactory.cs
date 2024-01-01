using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal sealed class ChatFactory(IAiAgentFactory aIAgentFactory, IMessenger messenger) : IChatFactory
    {
        public IChat CreateChat(string chatId)
        {
            return new Chat(chatId, aIAgentFactory.CreateAiAgent(), messenger);
        }
    }
}