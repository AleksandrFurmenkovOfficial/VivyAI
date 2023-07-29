using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VivyAI.Commands;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal class ChatCommandProcessor : IChatCommandProcessor
    {
        private readonly Dictionary<string, IChatCommand> commands = new();
        private readonly Func<IChatMessage, bool> isAdminChecker;

        public ChatCommandProcessor(Func<IChatMessage, bool> isAdminChecker)
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
        }

        private void AddCommand(IChatCommand command)
        {
            commands.Add($"/{command.CommandName}", command);
        }

        public async Task<bool> ExecuteIfChatCommand(IChatMessage message)
        {
            var text = message.content;
            foreach (var commandPair in commands.Where((value) => text.Trim().StartsWith(value.Key)))
            {
                var command = commandPair.Value;
                if (!command.IsAdminCommand || isAdminChecker(message))
                {
                    message.content = text[commandPair.Key.Length..];
                    return await command.Execute(message);
                }
            }

            return false;
        }
    }
}