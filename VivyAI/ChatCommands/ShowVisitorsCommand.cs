using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class ShowVisitorsCommand : IChatCommand
    {
        string IChatCommand.Name => "vis";
        bool IChatCommand.IsAdminCommand => true;
        public void Execute(IChat chat, IChatMessage message)
        {
            List<KeyValuePair<string, AppVisitor>> data = App.visitors.ToList();
            string vis = "Visitors:\n";
            foreach (KeyValuePair<string, AppVisitor> item in data)
            {
                vis += $"`{item.Key}` - {item.Value.who}:{item.Value.access}\n";
            }

            App.SendAppMessage(chat.Id, new ChatMessage()
            {
                Content = vis
            });
        }
    }
}