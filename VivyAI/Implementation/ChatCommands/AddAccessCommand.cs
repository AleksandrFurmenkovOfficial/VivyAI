using System.Collections.Concurrent;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatCommands
{
    internal sealed class AddAccessCommand : IChatCommand
    {
        private readonly ConcurrentDictionary<string, IAppVisitor> visitors;

        public AddAccessCommand(ConcurrentDictionary<string, IAppVisitor> visitors)
        {
            this.visitors = visitors;
        }

        string IChatCommand.Name => "add";
        bool IChatCommand.IsAdminOnlyCommand => true;

        public Task Execute(IChat chat, IChatMessage message)
        {
            _ = visitors.AddOrUpdate(chat.Id, _ =>
            {
                var arg = new AppVisitor(true, Strings.Unknown);
                return arg;
            }, (_, arg) =>
            {
                arg.Access = true;
                return arg;
            });

            var showVisitorsCommand = new ShowVisitorsCommand(visitors);
            return showVisitorsCommand.Execute(chat, message);
        }
    }
}