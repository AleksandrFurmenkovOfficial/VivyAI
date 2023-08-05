using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal class Chat : IChat
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly IOpenAI openAI;
        private readonly IMessanger messanger;
        private readonly List<IChatMessage> messages = new();
        private static readonly int step = 30;
        private int isCancelled = 0;

        public Chat(IOpenAI openAI, IMessanger messanger)
        {
            this.openAI = openAI;
            this.messanger = messanger;
        }

        public async Task DoResponseToMessage(IChatMessage message)
        {
            _ = Interlocked.Exchange(ref isCancelled, 0);
            messages.Add(message);
            await DoStreamResponseToLastMessage().ConfigureAwait(false);
        }

        private async Task DoStreamResponseToLastMessage()
        {
            var responseTargetMessage = new ChatMessage
            {
                chatId = messages.Last().chatId,
                role = "assistant",
                name = App.VivyName,
                content = "..."
            };
            messages.Add(responseTargetMessage);

            var newMessageId = await messanger.SendMessage(responseTargetMessage).ConfigureAwait(false);
            responseTargetMessage.content = "";

            int lastContentSize = 0;
            _ = await openAI.GetAIResponse(messages, async Task<bool> (contentDelta) =>
            {
                if (Interlocked.CompareExchange(ref isCancelled, 0, 0) > 0)
                {
                    return true;
                }

                responseTargetMessage.content += contentDelta;
                if (responseTargetMessage.content.Length % step == 0)
                {
                    await messanger.EditMessage(responseTargetMessage.chatId, newMessageId, responseTargetMessage.content).ConfigureAwait(false);
                    lastContentSize = responseTargetMessage.content.Length;
                }

                return false;
            }).ConfigureAwait(false);

            if (lastContentSize != responseTargetMessage.content.Length)
            {
                await messanger.EditMessage(responseTargetMessage.chatId, newMessageId, responseTargetMessage.content).ConfigureAwait(false);
            }
        }

        public void Reset()
        {
            messages.Clear();
        }

        public Task LockAsync()
        {
            _ = Interlocked.Exchange(ref isCancelled, 1);
            return semaphore.WaitAsync();
        }

        public void Unlock()
        {
            _ = semaphore.Release();
        }
    }
}