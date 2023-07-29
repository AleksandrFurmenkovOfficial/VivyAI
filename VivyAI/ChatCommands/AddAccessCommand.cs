using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Commands
{
    internal class AddAccessCommand : IChatCommand
    {
        string IChatCommand.CommandName => "add";
        bool IChatCommand.IsAdminCommand => true;
        public Task<bool> Execute(IChatMessage message)
        {
            _ = App.visitors.AddOrUpdate(message.chatId, (string id) => { Visitor arg = new(true, "Unknown"); return arg; }, (string id, Visitor arg) => { arg.access = true; return arg; });
            ShowVisitorsCommand nextCommand = new();
            return nextCommand.Execute(message);
        }
    }
}