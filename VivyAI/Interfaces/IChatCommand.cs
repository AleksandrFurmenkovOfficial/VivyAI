using System.Threading.Tasks;

namespace VivyAI.Interfaces
{
    internal interface IChatCommand
    {
        string CommandName { get; }
        bool IsAdminCommand { get; }
        Task<bool> Execute(IChatMessage message);
    }
}