using VivyAI.Interfaces;

namespace VivyAI.Implementation
{
    internal sealed class ChatMessageProcessor : IChatMessageProcessor
    {
        private readonly IChatCommandProcessor chatCommandProcessor;

        public ChatMessageProcessor(IChatCommandProcessor chatCommandProcessor)
        {
            this.chatCommandProcessor = chatCommandProcessor;
        }

        public async Task HandleMessage(IChat chat, IChatMessage message)
        {
            try
            {
                await chat.LockAsync().ConfigureAwait(false);
                bool isCommandDone =
                    await chatCommandProcessor.ExecuteIfChatCommand(chat, message).ConfigureAwait(false);
                if (isCommandDone)
                {
                    return;
                }

                await chat.DoResponseToMessage(message).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ExceptionHandler.LogException(e);
                await chat.SendSomethingGoesWrong().ConfigureAwait(false);
            }
            finally
            {
                chat.Unlock();
            }
        }
    }
}