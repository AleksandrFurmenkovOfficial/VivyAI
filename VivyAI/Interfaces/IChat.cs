using System.Threading.Tasks;

namespace VivyAI.Interfaces
{
    internal interface IChat
    {
        Task DoResponseToMessage(IChatMessage message);
        void Reset();
        Task LockAsync();
        void Unlock();
    }
}