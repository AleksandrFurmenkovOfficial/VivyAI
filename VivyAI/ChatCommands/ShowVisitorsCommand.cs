using System.Linq;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Commands
{
    internal class ShowVisitorsCommand : IChatCommand
    {
        string IChatCommand.CommandName => "vis";
        bool IChatCommand.IsAdminCommand => true;
        public Task<bool> Execute(IChatMessage message)
        {
            System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Visitor>> data = App.visitors.ToList();
            string vis = "Visitors:\n";
            foreach (System.Collections.Generic.KeyValuePair<string, Visitor> item in data)
            {
                vis += $"`{item.Key}` - {item.Value.who}:{item.Value.access}\n";
            }

            message.content = vis;
            App.SendAppMessage(message);
            return Task.FromResult(true);
        }
    }
}