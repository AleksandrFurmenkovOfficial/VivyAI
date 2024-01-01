using VivyAi.Implementation.ChatMessageActions;
using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal sealed partial class Chat : IChat, IDisposable
    {
        private const long NoInterruptionCode = 0;
        private const long StopCode = 1;
        private const int MessageUpdateStepInCharsCount = 42;

        private readonly IAiAgent aiAgent;

        private readonly List<List<IChatMessage>> messages = [];
        private readonly SemaphoreSlim messagesLock = new(1, 1);
        private readonly IMessenger messenger;

        private long interruptionCode = NoInterruptionCode;

        public Chat(string chatId, IAiAgent aiAgent, IMessenger messenger)
        {
            this.aiAgent = aiAgent;
            this.messenger = messenger;
            Id = chatId;

            SetCommonMode();
        }

        public string Id { get; }

        public async Task SendSomethingGoesWrong()
        {
            _ = await messenger.SendMessage(Id, new ChatMessage(Strings.SomethingGoesWrong),
                new List<ActionId> { RetryAction.Id }).ConfigureAwait(false);
        }

        public async Task DoResponseToMessage(IChatMessage message)
        {
            _ = Interlocked.Exchange(ref interruptionCode, NoInterruptionCode);
            await UpdateLastMessageButtons().ConfigureAwait(false);
            messages.Add([message]);
            await DoStreamResponseToLastMessage().ConfigureAwait(false);
        }

        public async Task Reset()
        {
            await UpdateLastMessageButtons().ConfigureAwait(false);
            messages.Clear();
        }

        public Task LockAsync()
        {
            _ = Interlocked.Exchange(ref interruptionCode, StopCode);
            return messagesLock.WaitAsync();
        }

        public void Unlock()
        {
            _ = messagesLock.Release();
        }

        public Task SendSystemMessage(string content)
        {
            return messenger.SendMessage(Id, new ChatMessage
            {
                Content = content
            });
        }

        public async Task RegenerateLastResponse()
        {
            _ = Interlocked.Exchange(ref interruptionCode, NoInterruptionCode);
            await RemoveResponse().ConfigureAwait(false);
            await DoStreamResponseToLastMessage().ConfigureAwait(false);
        }

        public async Task ContinueLastResponse()
        {
            _ = Interlocked.Exchange(ref interruptionCode, NoInterruptionCode);
            await DoResponseToMessage(new ChatMessage(IChatMessage.InternalMessageId, Strings.Continue,
                Strings.RoleSystem, Strings.RoleSystem)).ConfigureAwait(false);
        }

        public async Task RemoveResponse()
        {
            var lastPack = messages.Last();
            var initialUserInput = lastPack.First();
            foreach (var message in lastPack.Where(message => message.MessageId != IChatMessage.InternalMessageId &&
                                                              message != initialUserInput))
            {
                await messenger.DeleteMessage(Id, message.MessageId).ConfigureAwait(false);
            }

            lastPack.RemoveRange(1, lastPack.Count - 1);
        }

        public void Dispose()
        {
            messagesLock.Dispose();
        }

        private async Task UpdateLastMessageButtons()
        {
            if (messages.Count == 0)
            {
                return;
            }

            var lastMessages = messages.Last();
            var lastMessage = lastMessages.Last();

            if (lastMessage.Role != Strings.RoleAssistant)
            {
                return;
            }

            if (lastMessage.MessageId == IChatMessage.InternalMessageId)
            {
                return;
            }

            var content = (lastMessage.Content?.Length ?? 0) > 0 ? lastMessage.Content : Strings.InitAnswerTemplate;
            await UpdateMessage(lastMessage, content).ConfigureAwait(false);
        }

        private static bool IsMediaMessage(IChatMessage message)
        {
            return message.ImageUrl != null;
        }

        private async Task UpdateMessage(IChatMessage message, string newContent,
            IEnumerable<ActionId> newActions = null)
        {
            if (IsMediaMessage(message))
            {
                await messenger.EditMessageCaption(Id, message.MessageId, newContent, newActions).ConfigureAwait(false);
            }
            else
            {
                await messenger.EditTextMessage(Id, message.MessageId, newContent, newActions).ConfigureAwait(false);
            }
        }

        private IChatMessage CreateInitMessage()
        {
            return new ChatMessage
            {
                Role = Strings.RoleAssistant,
                Name = aiAgent.AiName,
                Content = Strings.InitAnswerTemplate
            };
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
                await aiAgent.GetResponse(Id, messages.SelectMany(subList => subList),
                    Task<bool> (contentDelta) =>
                        ProcessAsyncResponse(responseTargetMessage, contentDelta)).ConfigureAwait(false);
            }
            catch
            {
                await messenger.DeleteMessage(Id, responseTargetMessage.MessageId).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<bool> ProcessAsyncResponse(IChatMessage responseTargetMessage,
            ResponseStreamChunk contentDelta)
        {
            bool textStreamUpdate = contentDelta.Messages.Count == 0;
            var returnCode = Interlocked.Read(ref interruptionCode);
            var finalUpdate = contentDelta is LastResponseStreamChunk || returnCode == StopCode;

            if (textStreamUpdate || finalUpdate)
            {
                finalUpdate |=
                    !await UpdateTargetMessage(responseTargetMessage, contentDelta.TextDelta ?? "", finalUpdate)
                        .ConfigureAwait(false);

                if (!finalUpdate)
                {
                    return false;
                }

                AddAnswerMessage(responseTargetMessage);
                return true;
            }

            await ProcessFunctionResult(responseTargetMessage, contentDelta).ConfigureAwait(false);
            return true;
        }

        private async Task ProcessFunctionResult(IChatMessage responseTargetMessage, ResponseStreamChunk contentDelta)
        {
            var functionCallMessage = contentDelta.Messages.First();
            AddAnswerMessage(functionCallMessage);

            var functionResultMessage = contentDelta.Messages.Last();
            AddAnswerMessage(functionResultMessage);

            bool imageMessage = functionResultMessage.ImageUrl != null;
            if (imageMessage)
            {
                await messenger.DeleteMessage(Id, responseTargetMessage.MessageId).ConfigureAwait(false);
                var newMessageId = await messenger.SendPhotoMessage(Id, functionResultMessage.ImageUrl,
                    Strings.InitAnswerTemplate, [StopAction.Id]).ConfigureAwait(false);
                var responseTargetMessageNew = new ChatMessage(IChatMessage.InternalMessageId,
                    Strings.InitAnswerTemplate,
                    Strings.RoleAssistant, aiAgent.AiName, functionResultMessage.ImageUrl)
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

        private async Task<bool> UpdateTargetMessage(IChatMessage responseTargetMessage, string textContentDelta,
            bool finalUpdate)
        {
            responseTargetMessage.Content += textContentDelta;
            if (responseTargetMessage.Content.Length % MessageUpdateStepInCharsCount != 1 && !finalUpdate)
            {
                return true;
            }

            bool hasContent = responseTargetMessage.Content.Length > 0;
            bool hasMedia = responseTargetMessage.ImageUrl != null;
            switch (hasMedia)
            {
                case true when responseTargetMessage.Content.Length > IMessenger.MaxCaptionLen:
                    responseTargetMessage.Content = responseTargetMessage.Content[..IMessenger.MaxCaptionLen];
                    finalUpdate = true;
                    break;
                case false when responseTargetMessage.Content.Length > IMessenger.MaxTextLen:
                    responseTargetMessage.Content = responseTargetMessage.Content[..IMessenger.MaxTextLen];
                    finalUpdate = true;
                    break;
            }

            var newContent = hasContent ? responseTargetMessage.Content : Strings.SomethingGoesWrong;
            List<ActionId> actions = finalUpdate
                ? hasContent ? [ContinueAction.Id, RegenerateAction.Id] : [RetryAction.Id]
                : [StopAction.Id];
            await UpdateMessage(responseTargetMessage, newContent, actions).ConfigureAwait(false);

            return !finalUpdate;
        }

        private async Task<IChatMessage> SendResponseTargetMessage()
        {
            var responseTargetMessage = CreateInitMessage();
            responseTargetMessage.MessageId = await messenger
                .SendMessage(Id, responseTargetMessage, new List<ActionId> { CancelAction.Id })
                .ConfigureAwait(false);
            responseTargetMessage.Content = string.Empty;
            return responseTargetMessage;
        }
    }
}