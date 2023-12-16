using Newtonsoft.Json;
using System.Reflection;
using VivyAI.Interfaces;
using VivyAI.MessageActions;
using File = System.IO.File;

namespace VivyAI
{

    internal sealed class Chat : IChat, IDisposable
    {
        private const int messageUpdateStepInCharsCount = 42;

        private readonly string chatId;
        private readonly IAIAgent openAI;
        private readonly IMessanger messanger;

        private readonly List<List<IChatMessage>> messages = new();
        private readonly SemaphoreSlim messagesLock = new(1, 1);

        private long interruptionCode = IChat.noInterruptionCode;

        public string Id { get => chatId; }

        public Chat(string chatId, IAIAgent openAI, IMessanger messanger)
        {
            this.openAI = openAI;
            this.messanger = messanger;
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

        private async Task UpdateLastMessageButtons(bool isAdd = false)
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
            var actions = isAdd ? new List<ActionId> { new ActionId(ContinueAction.Name), new ActionId(RegenerateAction.Name) } : null;
            try // todo:
            {
                await messanger.EditTextMessage(chatId, lastMessage.MessageId, content, actions).ConfigureAwait(false);
            }
            catch
            {
                await messanger.EditMessageCaption(chatId, lastMessage.MessageId, content, actions).ConfigureAwait(false);
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
            _ = await messanger.DeleteMessage(chatId, messageId).ConfigureAwait(false);
        }

        private void AddAnswerMessage(IChatMessage responseTargetMessage)
        {
            messages.Last().Add(responseTargetMessage);
        }

        private async Task DoStreamResponseToLastMessage(IChatMessage responseTargetMessage = null, bool isMediaCaption = false)
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

                    return await ProcessAsyncResponse(responseTargetMessage, contentDelta, isMediaCaption).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await DeleteMessage(responseTargetMessage.MessageId).ConfigureAwait(false);
                throw;
            }
        }

        private async Task<bool> ProcessAsyncResponse(IChatMessage responseTargetMessage, ResponseStreamChunk contentDelta, bool isMediaCaption)
        {
            bool textStreamUpdate = contentDelta.messages.Count == 0;
            if (textStreamUpdate)
            {
                var returnCode = Interlocked.Read(ref interruptionCode);
                var finalUpdate = contentDelta.isEnd || returnCode == IChat.stopCode;
                await UpdateTargetMessage(responseTargetMessage, contentDelta.textStep, finalUpdate: finalUpdate, isMediaCaption: isMediaCaption).ConfigureAwait(false);
                if (finalUpdate)
                {
                    AddAnswerMessage(responseTargetMessage);
                    return true;
                }

                return false;
            }

            _ = await ProcessFunctionResult(responseTargetMessage, contentDelta).ConfigureAwait(false);
            return true;
        }

        private async Task<bool> ProcessFunctionResult(IChatMessage responseTargetMessage, ResponseStreamChunk contentDelta)
        {
            var functionResultMessage = contentDelta.messages.First();
            functionResultMessage.MessageId = IChatMessage.internalMessage;
            AddAnswerMessage(functionResultMessage);

            bool imageMessage = functionResultMessage.ImageUrl != null;
            if (imageMessage)
            {
                await DeleteMessage(responseTargetMessage.MessageId).ConfigureAwait(false);
                var responseTargetMessageNew = new ChatMessage(messageId: IChatMessage.internalMessage, Strings.InitAnswerTemplate, Strings.RoleAssistant, openAI.AIName)
                {
                    MessageId = await messanger.SendPhotoMessage(chatId, functionResultMessage.ImageUrl, Strings.InitAnswerTemplate, new List<ActionId> { new ActionId(StopAction.Name) }).ConfigureAwait(false),
                    Content = ""
                };
                await DoStreamResponseToLastMessage(responseTargetMessageNew, isMediaCaption: true).ConfigureAwait(false);
            }
            else
            {
                await DoStreamResponseToLastMessage(responseTargetMessage).ConfigureAwait(false);
            }

            return true;
        }

        private async Task UpdateTargetMessage(IChatMessage responseTargetMessage, string textContentDelta = "", bool finalUpdate = false, bool isMediaCaption = false)
        {
            responseTargetMessage.Content += textContentDelta;
            if (responseTargetMessage.Content.Length % messageUpdateStepInCharsCount == 0 || finalUpdate)
            {
                if (isMediaCaption)
                {
                    await messanger.EditMessageCaption(chatId, responseTargetMessage.MessageId, responseTargetMessage.Content, finalUpdate ? new List<ActionId> { new ActionId(ContinueAction.Name), new ActionId(RegenerateAction.Name) } : new List<ActionId> { new ActionId(StopAction.Name) }).ConfigureAwait(false);
                }
                else
                {
                    await messanger.EditTextMessage(chatId, responseTargetMessage.MessageId, responseTargetMessage.Content, finalUpdate ? new List<ActionId> { new ActionId(ContinueAction.Name), new ActionId(RegenerateAction.Name) } : new List<ActionId> { new ActionId(StopAction.Name) }).ConfigureAwait(false);
                }
            }
        }

        private async Task<IChatMessage> SendResponseTargetMessage()
        {
            var responseTargetMessage = CreateInitMessage();
            responseTargetMessage.MessageId = await messanger.SendMessage(chatId, responseTargetMessage, new List<ActionId> { new ActionId(CancelAction.Name) }).ConfigureAwait(false);
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

        private void AddMessageExample(string messageRole, string message)
        {
            if (messages.Count == 0)
                messages.Add(new List<IChatMessage>());

            AddAnswerMessage(new ChatMessage
            {
                MessageId = IChatMessage.internalMessage,
                Role = messageRole,
                Name = messageRole == Strings.RoleUser ? $"{messageRole}Name" : openAI.AIName,
                Content = message
            });
        }

        private void SetMode(string modeDescriptionFilename)
        {
            string jsonText = File.ReadAllText(modeDescriptionFilename);
            dynamic jsonObj = JsonConvert.DeserializeObject(jsonText);

            openAI.EnableFunctions = jsonObj?.EnableFunctions?.Value ?? false;
            var name = jsonObj?.AIName?.Value.Trim();
            openAI.AIName = string.IsNullOrEmpty(name) ? Strings.DefaultName : name;
            var settings = jsonObj?.AISettings?.Value?.Trim();
            openAI.SystemMessage = string.IsNullOrEmpty(settings) ? Strings.DefaultDescription : settings;

            if (jsonObj.Examples != null)
            {
                foreach (var example in jsonObj.Examples)
                {
                    if (example != null && example?.Role != null && example?.Message != null)
                    {
                        AddMessageExample(example.Role.Value, example.Message.Value);
                    }
                }
            }
        }

        private static string GetPath(string mode)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return $"{directory}/Modes/{mode}.json";
        }

        public void SetEnglishTeacherMode()
        {
            SetMode(GetPath("EnglishTeacherMode"));
        }

        public void SetCommonMode()
        {
            SetMode(GetPath("CommonMode"));
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