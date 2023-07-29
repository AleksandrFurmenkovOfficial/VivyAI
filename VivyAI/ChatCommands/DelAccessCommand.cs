using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Commands
{
    internal class DelAccessCommand : IChatCommand
    {
        string IChatCommand.CommandName => "del";
        bool IChatCommand.IsAdminCommand => true;
        public Task<bool> Execute(IChatMessage message)
        {
            _ = App.visitors.AddOrUpdate(message.chatId, (string id) => { Visitor arg = new(false, "Unknown"); return arg; }, (string id, Visitor arg) => { arg.access = false; return arg; });
            ShowVisitorsCommand nextCommand = new();
            return nextCommand.Execute(message);
        }
    }
}