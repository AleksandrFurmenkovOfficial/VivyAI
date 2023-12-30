using System.Collections.Concurrent;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.ChatCommands
{
    internal sealed class ChatCommandProcessor : IChatCommandProcessor
    {
        private readonly Dictionary<string, IChatCommand> commands = new();
        private readonly IAdminChecker isAdminChecker;

        public ChatCommandProcessor(
            IAdminChecker isAdminChecker,
            ConcurrentDictionary<string, IAppVisitor> visitors)
        {
            this.isAdminChecker = isAdminChecker;
            RegisterCommands(visitors);
        }

        public async Task<bool> ExecuteIfChatCommand(IChat chat, IChatMessage message)
        {
            if (string.IsNullOrEmpty(message.Content))
            {
                return false;
            }

            var text = message.Content;
            foreach (var commandPair in commands.Where(value =>
                         text.Trim().Contains(value.Key, StringComparison.InvariantCultureIgnoreCase)))
            {
                var command = commandPair.Value;
                if (!command.IsAdminOnlyCommand || isAdminChecker.IsAdmin(chat.Id))
                {
                    message.Content = text[commandPair.Key.Length..];
                    await command.Execute(chat, message).ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }

        private void RegisterCommands(ConcurrentDictionary<string, IAppVisitor> visitors)
        {
            void AddCommand(IChatCommand command)
            {
                commands.Add($"/{command.Name}", command);
            }

            AddCommand(new StartCommand());
            AddCommand(new ShowVisitorsCommand(visitors));
            AddCommand(new AddAccessCommand(visitors));
            AddCommand(new DelAccessCommand(visitors));
            AddCommand(new CommonCommand());
            AddCommand(new EnglishCommand());
        }
    }
}