using VivyAI.Interfaces;

namespace VivyAI.Commands
{
    internal sealed class AddAccessCommand : IChatCommand
    {
        string IChatCommand.CommandName => "add";
        bool IChatCommand.IsAdminCommand => true;
        public void Execute(IChat chat, IChatMessage message)
        {
            _ = App.visitors.AddOrUpdate(chat.Id, (string id) => { AppVisitor arg = new(true, "Unknown"); return arg; }, (string id, AppVisitor arg) => { arg.access = true; return arg; });
            var nextCommand = new ShowVisitorsCommand();
            nextCommand.Execute(chat, message);
        }
    }
}