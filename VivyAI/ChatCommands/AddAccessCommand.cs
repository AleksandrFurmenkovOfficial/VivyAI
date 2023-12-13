using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class AddAccessCommand : IChatCommand
    {
        string IChatCommand.CommandName => "add";
        bool IChatCommand.IsAdminCommand => true;
        public void Execute(IChat chat, IChatMessage message)
        {
            _ = App.visitors.AddOrUpdate(chat.Id, (id) => { AppVisitor arg = new(true, "Unknown"); return arg; }, (id, arg) => { arg.access = true; return arg; });
            var nextCommand = new ShowVisitorsCommand();
            nextCommand.Execute(chat, message);
        }
    }
}