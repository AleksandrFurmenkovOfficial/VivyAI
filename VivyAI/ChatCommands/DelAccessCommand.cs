using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class DelAccessCommand : IChatCommand
    {
        string IChatCommand.CommandName => "del";
        bool IChatCommand.IsAdminCommand => true;
        public void Execute(IChat chat, IChatMessage message)
        {
            _ = App.visitors.AddOrUpdate(chat.Id, (id) => { AppVisitor arg = new(false, "Unknown"); return arg; }, (id, arg) => { arg.access = false; return arg; });
            var nextCommand = new ShowVisitorsCommand();
            nextCommand.Execute(chat, message);
        }
    }
}