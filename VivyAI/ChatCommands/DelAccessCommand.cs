using VivyAI.Interfaces;

namespace VivyAI.Commands
{
    internal sealed class DelAccessCommand : IChatCommand
    {
        string IChatCommand.CommandName => "del";
        bool IChatCommand.IsAdminCommand => true;
        public void Execute(IChat chat, IChatMessage message)
        {
            _ = App.visitors.AddOrUpdate(chat.Id, (string id) => { AppVisitor arg = new(false, "Unknown"); return arg; }, (string id, AppVisitor arg) => { arg.access = false; return arg; });
            var nextCommand = new ShowVisitorsCommand();
            nextCommand.Execute(chat, message);
        }
    }
}