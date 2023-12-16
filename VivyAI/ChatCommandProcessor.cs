using VivyAI.ChatCommands;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed class ChatCommandProcessor : IChatCommandProcessor
    {
        private readonly Dictionary<string, IChatCommand> commands = new();
        private readonly Func<string, bool> isAdminChecker;

        public ChatCommandProcessor(Func<string, bool> isAdminChecker)
        {
            this.isAdminChecker = isAdminChecker;
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            AddCommand(new StartCommand());
            AddCommand(new ShowVisitorsCommand());
            AddCommand(new AddAccessCommand());
            AddCommand(new DelAccessCommand());
            AddCommand(new CommonCommand());
            AddCommand(new EnglishCommand());
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
            foreach (var commandPair in commands.Where((value) => text.Trim().StartsWith(value.Key, StringComparison.OrdinalIgnoreCase)))
            {
                var command = commandPair.Value;
                if (!command.IsAdminCommand || isAdminChecker(chatId))
                {
                    message.Content = text[commandPair.Key.Length..];
                    if (App.chatById.TryGetValue(chatId, out IChat chat))
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