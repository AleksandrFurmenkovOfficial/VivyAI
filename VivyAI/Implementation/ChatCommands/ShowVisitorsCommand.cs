using System.Collections.Concurrent;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatCommands
{
    internal sealed class ShowVisitorsCommand : IChatCommand
    {
        private readonly ConcurrentDictionary<string, IAppVisitor> visitors;

        public ShowVisitorsCommand(ConcurrentDictionary<string, IAppVisitor> visitors)
        {
            this.visitors = visitors;
        }

        string IChatCommand.Name => "vis";
        bool IChatCommand.IsAdminOnlyCommand => true;

        public Task Execute(IChat chat, IChatMessage message)
        {
            string vis = "Visitors:\n";
            foreach (var item in visitors)
            {
                vis += $"{item.Key} - {item.Value.Name}:{item.Value.Access}\n";
            }

            return chat.SendSystemMessage(vis);
        }
    }
}