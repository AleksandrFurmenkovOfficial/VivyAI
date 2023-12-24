using System.Collections.Concurrent;
using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class AddAccessCommand : IChatCommand
    {
        string IChatCommand.Name => "add";
        bool IChatCommand.IsAdminCommand => true;
        private readonly ConcurrentDictionary<string, AppVisitor> visitors;
        private readonly IMessenger Messenger;
        public AddAccessCommand(ConcurrentDictionary<string, AppVisitor> visitors, IMessenger Messenger)
        {
            this.visitors = visitors;
            this.Messenger = Messenger;
        }
        public void Execute(IChat chat, IChatMessage message)
        {
            _ = visitors.AddOrUpdate(chat.Id, (id) => { AppVisitor arg = new(true, Strings.Unknown); return arg; }, (id, arg) => { arg.access = true; return arg; });
            var showVisitorsCommand = new ShowVisitorsCommand(visitors, Messenger);
            showVisitorsCommand.Execute(chat, message);
        }
    }
}