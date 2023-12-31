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
            foreach ((string commandName, IChatCommand command) in commands.Where(value =>
                         text.Trim().Contains(value.Key, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (command.IsAdminOnlyCommand && !isAdminChecker.IsAdmin(chat.Id))
                    return false;

                message.Content = text[commandName.Length..];
                await command.Execute(chat, message).ConfigureAwait(false);
                return true;
            }

            return false;
        }

        private void RegisterCommands(ConcurrentDictionary<string, IAppVisitor> visitors)
        {
            AddCommand(new StartCommand());
            AddCommand(new ShowVisitorsCommand(visitors));
            AddCommand(new AddAccessCommand(visitors));
            AddCommand(new DelAccessCommand(visitors));
            AddCommand(new CommonCommand());
            AddCommand(new EnglishCommand());
            return;

            void AddCommand(IChatCommand command)
            {
                commands.Add($"/{command.Name}", command);
            }
        }
    }
}