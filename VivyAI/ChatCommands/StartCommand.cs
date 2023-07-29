using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Commands
{
    internal class StartCommand : IChatCommand
    {
        string IChatCommand.CommandName => "start";
        bool IChatCommand.IsAdminCommand => false;
        public async Task<bool> Execute(IChatMessage message)
        {
            if (App.chatById.TryGetValue(message.chatId, out IChat chatContext))
            {
                chatContext.Reset();

                ChatMessage commandDone = new()
                {
                    chatId = message.chatId,
                    content = Strings.Warning
                };

                App.SendAppMessage(commandDone);
            }

            return await Task.FromResult(true);
        }
    }
}