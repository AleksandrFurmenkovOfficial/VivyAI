using System.Collections.Concurrent;
using VivyAI.ChatCommands;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed class ChatCommandProce : IChatCommandProcessor
    {
        private readonly Dictionary<string, IChatCommand> commands = new();
        private readonly Func<string, bool> isAdminChecker;
        private readonly ConcurrentDictionary<string, IChat> chatById;

        public ChatCommandProce(
            Func<string, bool> isAdminChecker,
            ConcurrentDictionary<string, AppVisitor> visitors,
            ConcurrentDictionary<string, IChat> chatById,
            IMessenger Messenger)
        {
            this.isAdminChecker = isAdminChecker;
            this.chatById = chatById;

            AddCommand(new StartCommand(Messenger));
            AddCommand(new ShowVisitorsCommand(visitors, Messenger));
            AddCommand(new AddAccessCommand(visitors, Messenger));
            AddCommand(new DelAccessCommand(visitors, Messenger));
            AddCommand(new CommonCommand(Messenger));
            AddCommand(new EnglishCommand(Messenger));
        }

        private void AddCommand(IChatCommand command)
        {
            commands.Add($"/{command.Name}", command);
        }

        public bool ExecuteIfChatCommand(string chatId, IChatMessage message)
        {
            if (string.IsNullOrEmpty(message.Content))
            {
                return false;
            }

            var text = message.Content;
            foreach (var commandPair in commands.Where((value) => text.Trim().Contains(value.Key, StringComparison.InvariantCultureIgnoreCase)))
            {
                var command = commandPair.Value;
                if (!command.IsAdminCommand || isAdminChecker(chatId))
                {
                    message.Content = text[commandPair.Key.Length..];
                    if (chatById.TryGetValue(chatId, out IChat chat))
                    {
                        command.Execute(chat, message);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}