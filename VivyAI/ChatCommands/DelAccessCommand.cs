using System.Collections.Concurrent;
using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class DelAccessCommand : IChatCommand
    {
        string IChatCommand.Name => "del";
        bool IChatCommand.IsAdminCommand => true;
        private readonly ConcurrentDictionary<string, AppVisitor> visitors;
        private readonly IMessenger Messenger;
        public DelAccessCommand(ConcurrentDictionary<string, AppVisitor> visitors, IMessenger Messenger)
        {
            this.visitors = visitors;
            this.Messenger = Messenger;
        }
        public void Execute(IChat chat, IChatMessage message)
        {
            _ = visitors.AddOrUpdate(chat.Id, (id) => { AppVisitor arg = new(false, Strings.Unknown); return arg; }, (id, arg) => { arg.access = false; return arg; });
            var showVisitorsCommand = new ShowVisitorsCommand(visitors, Messenger);
            showVisitorsCommand.Execute(chat, message);
        }
    }
}