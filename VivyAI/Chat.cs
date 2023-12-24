using VivyAI.Interfaces;
using VivyAI.MessageActions;

namespace VivyAI
{
    internal sealed partial class Chat : IChat, IDisposable
    {
        private const int messageUpdateStepInCharsCount = 42;

        private readonly string chatId;
        private readonly IAIAgent openAI;
        private readonly IMessenger Messenger;

        private readonly List<List<IChatMessage>> messages = new();
        private readonly SemaphoreSlim messagesLock = new(1, 1);

        private long interruptionCode = IChat.noInterruptionCode;

        public string Id { get => chatId; }

        public Chat(string chatId, IAIAgent openAI, IMessenger Messenger)
        {
            this.openAI = openAI;
            this.Messenger = Messenger;
            this.chatId = chatId;

            SetCommonMode();
        }

        public async Task DoResponseToMessage(IChatMessage message)
        {
            _ = Interlocked.Exchange(ref interruptionCode, IChat.noInterruptionCode);
            await UpdateLastMessageButtons().ConfigureAwait(false);
            messages.Add(new List<IChatMessage> { message });
            await DoStreamResponseToLastMessage().ConfigureAwait(false);
        }

        private async Task UpdateLastMessageButtons()
        {
            if (messages.Count == 0)
                return;

            var lastMessages = messages.Last();
            var lastMessage = lastMessages.Last();

            if (lastMessage.Role != Strings.RoleAssistant)
                return;

            if (lastMessage.MessageId == IChatMessage.internalMessage)
                return;

            var content = (lastMessage.Content?.Length ?? 0) > 0 ? lastMessage.Content : Strings.InitAnswerTemplate;
            await UpdateMessage(lastMessage, content).ConfigureAwait(false);
        }

        private static bool IsMediaMessage(IChatMessage message)
        {
            return message.ImageUrl != null;
        }

        private async Task UpdateMessage(IChatMessage message, string newContent, List<ActionId> newActions = null)
        {
            if (IsMediaMessage(message))
            {
                await Messenger.EditMessageCaption(chatId, message.MessageId, newContent, newActions).ConfigureAwait(false);
            }
            else
            {
                await Messenger.EditTextMessage(chatId, message.MessageId, newContent, newActions).ConfigureAwait(false);
            }
        }

        private IChatMessage CreateInitMessage()
        {
            return new ChatMessage
            {
                Role = Strings.RoleAssistant,
                Name = openAI.AIName,
                Content = Strings.InitAnswerTemplate
            };
        }

        private async Task DeleteMessage(string messageId)
        {
            _ = await Messenger.DeleteMessage(chatId, messageId).ConfigureAwait(false);
        }

        private void AddAnswerMessage(IChatMessage responseTargetMessage)
        {
            messages.Last().Add(responseTargetMessage);
        }

        private async Task DoStreamResponseToLastMessage(IChatMessage responseTargetMessage = null)
        {
            responseTargetMessage ??= await SendResponseTargetMessage().ConfigureAwait(false);
            try
            {
                await openAI.GetAIResponse(messages.SelectMany(subList => subList).ToList(), async Task<bool> (contentDelta) =>
                {
                    var returnCode = Interlocked.Read(ref interruptionCode);
                    if (returnCode == IChat.cancelCode)
                    {
                        await DeleteMessage(responseTargetMessage.MessageId).ConfigureAwait(false);
                        return true;
                    }

                    return await ProcessAsyncResponse(responseTargetMessage, contentDelta).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DeleteMessage(responseTargetMessage.MessageId).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<bool> ProcessAsyncResponse(IChatMessage responseTargetMessage, ResponseStreamChunk contentDelta)
        {
            bool textStreamUpdate = contentDelta.messages.Count == 0;
            if (textStreamUpdate)
            {
                var returnCode = Interlocked.Read(ref interruptionCode);
                var finalUpdate = contentDelta.isEnd || returnCode == IChat.stopCode;
                finalUpdate |= !await UpdateTargetMessage(responseTargetMessage, contentDelta.textStep, finalUpdate: finalUpdate).ConfigureAwait(false);
                if (finalUpdate)
                {
                    AddAnswerMessage(responseTargetMessage);
                    return true;
                }

                return false;
            }

            await ProcessFunctionResult(responseTargetMessage, contentDelta).ConfigureAwait(false);
            return true;
        }

        private async Task ProcessFunctionResult(IChatMessage responseTargetMessage, ResponseStreamChunk contentDelta)
        {
            var functionCallMessage = contentDelta.messages.First();
            AddAnswerMessage(functionCallMessage);

            var functionResultMessage = contentDelta.messages.Last();
            AddAnswerMessage(functionResultMessage);

            bool imageMessage = functionResultMessage.ImageUrl != null;
            if (imageMessage)
            {
                await DeleteMessage(responseTargetMessage.MessageId).ConfigureAwait(false);
                var newMessageId = await Messenger.SendPhotoMessage(chatId, functionResultMessage.ImageUrl, Strings.InitAnswerTemplate, new List<ActionId> { new ActionId(StopAction.Name) }).ConfigureAwait(false);
                var responseTargetMessageNew = new ChatMessage(messageId: IChatMessage.internalMessage, Strings.InitAnswerTemplate, Strings.RoleAssistant, openAI.AIName, functionResultMessage.ImageUrl)
                {
                    Content = "",
                    MessageId = newMessageId
                };
                await DoStreamResponseToLastMessage(responseTargetMessageNew).ConfigureAwait(false);
            }
            else
            {
                await DoStreamResponseToLastMessage(responseTargetMessage).ConfigureAwait(false);
            }
        }

        private async Task<bool> UpdateTargetMessage(IChatMessage responseTargetMessage, string textContentDelta, bool finalUpdate)
        {
            responseTargetMessage.Content += textContentDelta;
            if ((responseTargetMessage.Content.Length % messageUpdateStepInCharsCount) == 1 || finalUpdate)
            {
                bool hasContent = responseTargetMessage.Content.Length > 0;
                bool hasMedia = responseTargetMessage.ImageUrl != null;
                if (hasMedia && responseTargetMessage.Content.Length > IMessenger.maxCaptionLen)
                {
                    responseTargetMessage.Content = responseTargetMessage.Content[..IMessenger.maxCaptionLen];
                    finalUpdate = true;
                }
                else if (!hasMedia && responseTargetMessage.Content.Length > IMessenger.maxTextLen)
                {
                    responseTargetMessage.Content = responseTargetMessage.Content[..IMessenger.maxTextLen];
                    finalUpdate = true;
                }

                var newContent = hasContent ? (string)responseTargetMessage.Content.Clone() : "..."; // TODO: goesWrong
                var actions = finalUpdate ? (hasContent ? new List<ActionId> { new ActionId(ContinueAction.Name), new ActionId(RegenerateAction.Name) } : new List<ActionId> { new ActionId(RetryAction.Name) }) : new List<ActionId> { new ActionId(StopAction.Name) };
                await UpdateMessage(responseTargetMessage, newContent, actions).ConfigureAwait(false);

                return !finalUpdate;
            }

            return true;
        }

        private async Task<IChatMessage> SendResponseTargetMessage()
        {
            var responseTargetMessage = CreateInitMessage();
            responseTargetMessage.MessageId = await Messenger.SendMessage(chatId, responseTargetMessage, new List<ActionId> { new ActionId(CancelAction.Name) }).ConfigureAwait(false);
            responseTargetMessage.Content = "";
            return responseTargetMessage;
        }

        public async void Reset()
        {
            await UpdateLastMessageButtons().ConfigureAwait(false);
            messages.Clear();
        }

        public Task LockAsync(long lockCode)
        {
            _ = Interlocked.Exchange(ref interruptionCode, lockCode);
            return messagesLock.WaitAsync();
        }

        public void Unlock()
        {
            _ = messagesLock.Release();
        }

        public async void Regenerate(string messageId)
        {
            await RemoveResponse().ConfigureAwait(false);
            await DoStreamResponseToLastMessage().ConfigureAwait(false);
        }

        private async Task RemoveResponse()
        {
            var lastPack = messages.Last();
            var initialUserInput = lastPack.First();
            foreach (var message in lastPack)
            {
                if (message.MessageId != IChatMessage.internalMessage && message.MessageId != initialUserInput.MessageId)
                    await DeleteMessage(message.MessageId).ConfigureAwait(false);
            }

            lastPack.RemoveRange(1, lastPack.Count - 1);
        }

        public async void Continue()
        {
            await DoResponseToMessage(new ChatMessage(messageId: IChatMessage.internalMessage, Strings.Continue, Strings.RoleSystem, Strings.RoleSystem)).ConfigureAwait(false);
        }

        public void Dispose()
        {
            messagesLock.Dispose();
        }
    }
}