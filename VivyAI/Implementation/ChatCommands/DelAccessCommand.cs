using System.Collections.Concurrent;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatCommands
{
    internal sealed class DelAccessCommand : IChatCommand
    {
        private readonly ConcurrentDictionary<string, IAppVisitor> visitors;

        public DelAccessCommand(ConcurrentDictionary<string, IAppVisitor> visitors)
        {
            this.visitors = visitors;
        }

        string IChatCommand.Name => "del";
        bool IChatCommand.IsAdminOnlyCommand => true;

        public Task Execute(IChat chat, IChatMessage message)
        {
            _ = visitors.AddOrUpdate(chat.Id, _ =>
            {
                var arg = new AppVisitor(false, Strings.Unknown);
                return arg;
            }, (_, arg) =>
            {
                arg.Access = false;
                return arg;
            });
            var showVisitorsCommand = new ShowVisitorsCommand(visitors);
            return showVisitorsCommand.Execute(chat, message);
        }
    }
}