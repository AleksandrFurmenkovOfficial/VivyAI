using System.Threading.Tasks;

namespace VivyAI.Interfaces
{
    internal interface IChatCommandProcessor
    {
        public Task<bool> ExecuteIfChatCommand(IChatMessage message);
    }
}