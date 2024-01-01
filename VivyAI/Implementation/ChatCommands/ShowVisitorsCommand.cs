using System.Collections.Concurrent;
using VivyAi.Interfaces;

namespace VivyAi.Implementation.ChatCommands
{
    internal sealed class ShowVisitorsCommand(ConcurrentDictionary<string, IAppVisitor> visitors) : IChatCommand
    {
        string IChatCommand.Name => "vis";
        bool IChatCommand.IsAdminOnlyCommand => true;

        public Task Execute(IChat chat, IChatMessage message)
        {
            string vis = visitors.Aggregate("Visitors:\n",
                (current, item) => current + $"{item.Key} - {item.Value.Name}:{item.Value.Access}\n");
            return chat.SendSystemMessage(vis);
        }
    }
}