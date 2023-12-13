using Newtonsoft.Json;
using System.Reflection;
using VivyAI.Interfaces;
using VivyAI.MessageCallbacks;
using File = System.IO.File;

namespace VivyAI
{
    internal sealed class Chat : IChat, IDisposable
    {
        private const int messageUpdateStepInCharsCount = 42;

        private readonly string chatId;
        private readonly IOpenAI openAI;
        private readonly IMessanger messanger;

        private readonly List<IChatMessage> messages = new();
        private readonly SemaphoreSlim semaphore = new(1, 1);

        private int interruptionCode = IChat.noInterruptionCode;

        public string Id { get => chatId; }

        public Chat(string chatId, IOpenAI openAI, IMessanger messanger)
        {
            this.openAI = openAI;
            this.messanger = messanger;
            this.chatId = chatId;

            SetCommonMode();
        }

        public async Task DoResponseToMessage(IChatMessage message)
        {
            _ = Interlocked.Exchange(ref interruptionCode, IChat.noInterruptionCode);
            await RemoveButtonsFromLastAssistantMessage();
            messages.Add(message);
            await DoStreamResponseToLastMessage();
        }

        private async Task RemoveButtonsFromLastAssistantMessage()
        {
            if (messages.Count == 0)
                return;

            var lastMessage = messages.Last();
            if (lastMessage.Role != Strings.RoleAssistant)
                return;

            if (lastMessage.Id == IChatMessage.internalMessage)
                return;

            await messanger.EditTextMessage(chatId, lastMessage.Id, (lastMessage.Content?.Length ?? 0) > 0 ? lastMessage.Content : Strings.InitAnswerTemplate);
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
            _ = await messanger.DeleteMessage(chatId, messageId);
        }

        private async Task DoStreamResponseToLastMessage(string messageId = null)
        {
            bool noAnswer = false;
            var responseTargetMessage = await GetResponseTargetMessage(messageId);
            _ = await openAI.GetAIResponse(messages, async Task<bool> (contentDelta) =>
            {
                var returnCode = Interlocked.CompareExchange(ref interruptionCode, IChat.noInterruptionCode, IChat.noInterruptionCode);
                if (returnCode > 0)
                {
                    if (returnCode == IChat.stopCode)
                    {
                        await UpdateTargetMessage(contentDelta, responseTargetMessage, force: true, final: true);
                        messages.Add(responseTargetMessage);
                    }

                    if (returnCode == IChat.cancelCode)
                    {
                        noAnswer = true;
                        await DeleteMessage(responseTargetMessage.Id);
                    }

                    return true;
                }

                if (contentDelta.messages.Count == 0)
                {
                    await UpdateTargetMessage(contentDelta, responseTargetMessage);
                    return false;
                }

                var resultMessage = contentDelta.messages.First();
                responseTargetMessage.Content = resultMessage?.Content ?? "";
                messages.Add(resultMessage);
                noAnswer = true;
                await DeleteMessage(responseTargetMessage.Id);
                bool imageMessage = resultMessage.ImageUrl != null;
                if (imageMessage)
                {
                    _ = await messanger.SendPhotoMessage(chatId, resultMessage.ImageUrl);
                }

                await DoStreamResponseToLastMessage(); // todo:
                return true;
            });

            if (!noAnswer && responseTargetMessage.Content.Length > 0)
            {
                await messanger.EditTextMessage(chatId, responseTargetMessage.Id, responseTargetMessage.Content, new List<CallbackId> { new CallbackId(ContinueCallback.cName), new CallbackId(RegenerateCallback.cName) });
                messages.Add(responseTargetMessage);
            }
        }

        private async Task UpdateTargetMessage(AnswerStreamStep contentDelta, IChatMessage responseTargetMessage, bool force = false, bool final = false)
        {
            responseTargetMessage.Content += contentDelta.textStep;
            if (responseTargetMessage.Content.Length % messageUpdateStepInCharsCount == 0 || force)
            {
                await messanger.EditTextMessage(chatId, responseTargetMessage.Id, responseTargetMessage.Content, final ? new List<CallbackId> { new CallbackId(ContinueCallback.cName), new CallbackId(RegenerateCallback.cName) } : new List<CallbackId> { new CallbackId(StopCallback.cName) });
            }
        }

        private async Task<IChatMessage> GetResponseTargetMessage(string messageId = null)
        {
            var responseTargetMessage = CreateInitMessage();
            if (string.IsNullOrEmpty(messageId))
            {
                responseTargetMessage.Id = await messanger.SendMessage(chatId, responseTargetMessage, new List<CallbackId> { new CallbackId(CancelCallback.cName) });
            }

            responseTargetMessage.Content = "";
            return responseTargetMessage;
        }

        public async void Reset()
        {
            await RemoveButtonsFromLastAssistantMessage();
            messages.Clear();
        }

        public Task LockAsync(int lockCode)
        {
            _ = Interlocked.Exchange(ref interruptionCode, lockCode);
            return semaphore.WaitAsync();
        }

        public void Unlock()
        {
            _ = semaphore.Release();
        }

        private void AddMessageExample(string messageRole, string message)
        {
            messages.Add(new ChatMessage
            {
                Id = IChatMessage.internalMessage,
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

        public void Stop()
        {
            _ = Interlocked.Exchange(ref interruptionCode, IChat.stopCode);
        }

        public void Cancel()
        {
            _ = Interlocked.Exchange(ref interruptionCode, IChat.cancelCode);
        }

        public async void Regenerate(string messageId)
        {
            await DeleteMessage(messageId);
            _ = messages.RemoveAll(message => message.Id == messageId);
            await DoStreamResponseToLastMessage();
        }

        public async void Continue()
        {
            await DoResponseToMessage(new ChatMessage(id: IChatMessage.internalMessage, Strings.Continue, Strings.RoleSystem, Strings.RoleSystem));
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }
    }
}