using System.Collections.Concurrent;
using VivyAI.Interfaces;

namespace VivyAI.ChatCommands
{
    internal sealed class ShowVisitorsCommand : IChatCommand
    {
        string IChatCommand.Name => "vis";
        bool IChatCommand.IsAdminCommand => true;
        private readonly ConcurrentDictionary<string, AppVisitor> visitors;
        private readonly IMessenger Messenger;
        public ShowVisitorsCommand(ConcurrentDictionary<string, AppVisitor> visitors, IMessenger Messenger)
        {
            this.visitors = visitors;
            this.Messenger = Messenger;
        }
        public async void Execute(IChat chat, IChatMessage message)
        {
            List<KeyValuePair<string, AppVisitor>> data = visitors.ToList();
            string vis = "Visitors:\n";
            foreach (KeyValuePair<string, AppVisitor> item in data)
            {
                vis += $"{item.Key} - {item.Value.who}:{item.Value.access}\n";
            }

            _ = await Messenger.SendMessage(chat.Id, new ChatMessage()
            {
                Content = vis
            }).ConfigureAwait(false);
        }
    }
}