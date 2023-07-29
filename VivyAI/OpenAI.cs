using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.ChatFunctions;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VivyAI.Functions;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal class OpenAI : IOpenAI
    {
        private readonly OpenAIAPI api;
        private readonly List<Function> functionsList;
        private readonly Dictionary<string, IFunction> functions;
        private static readonly ThreadLocal<Random> random = new(() => new Random(Guid.NewGuid().GetHashCode()));

        private static string gptWithFunctionsLast = Model.GPT4_0613;
        private static string gptLast = Model.GPT4;
        private static string gptWideContext = "gpt-3.5-turbo-16k";

        public OpenAI(string token, string organization = "")
        {
            api = new OpenAIAPI(token);
            functionsList = new List<Function>();
            functions = new Dictionary<string, IFunction>();

            RegisterAIFunctions();
        }

        private void RegisterAIFunctions()
        {
            AddFunction(new ExtractDataFromTextFuncttion());
            AddFunction(new ExtractInformationFromURLFunction());
            AddFunction(new RetrieveAnswerFromVivyMemoryFunction());

            AddFunction(new WriteToVivyDiaryFunction());
            AddFunction(new ReadVivyDiaryFunction());
        }

        private void AddFunction(IFunction function)
        {
            functionsList.Add((Function)function.Description());
            functions.Add(function.name, function);
        }

        private async Task<bool> CallFunction(string functionName,
                                              string functionArguments,
                                              string userId,
                                              List<OpenAI_API.Chat.ChatMessage> messages,
                                              Func<string, Task<bool>> streamGetter)
        {
            try
            {
                var args = JsonConvert.DeserializeObject(functionArguments);
                var result = await functions[functionName].Call(this, args, userId);

                Console.WriteLine($"{functionName}({functionArguments})");
                messages.Add(new OpenAI_API.Chat.ChatMessage { Role = ChatMessageRole.Function, Content = "{{\"result\": {\"" + JsonConvert.SerializeObject(result) + "\"} }}", Name = functionName });
            }
            catch (Exception ex)
            {
                messages.Add(new OpenAI_API.Chat.ChatMessage
                {
                    Role = ChatMessageRole.Function,
                    Content = "{{\"result\": {\"Exception: Can't call function " + functionName + "(" + functionArguments + "); Exception message: " + ex.Message + "\"} }}",
                    Name = functionName
                });
                Console.WriteLine($"{messages.Last().Content})");
            }

            return await GetAIResponseImpl(messages, streamGetter, userId).ConfigureAwait(false);
        }

        private List<OpenAI_API.Chat.ChatMessage> ConvertMessages(List<IChatMessage> messages)
        {
            return messages.Select(message =>
            {
                OpenAI_API.Chat.ChatMessage result = new(ChatMessageRole.FromString(message.role), message.content);
                result.Name = message.name;
                return result;
            }).ToList();
        }

        public async Task<bool> GetAIResponse(List<IChatMessage> messages, Func<string, Task<bool>> streamGetter)
        {
            var convertedMessages = ConvertMessages(messages);
            return await GetAIResponseImpl(convertedMessages, streamGetter, messages.Last().chatId).ConfigureAwait(false);
        }

        private async Task<bool> GetAIResponseImpl(List<OpenAI_API.Chat.ChatMessage> convertedMessages, Func<string, Task<bool>> streamGetter, string userId)
        {
            try
            {
                var chatRequest = new ChatRequest()
                {
                    Model = gptWithFunctionsLast,
                    Functions = functionsList,
                    Temperature = random.Value.NextDouble() / 2,
                    Messages = convertedMessages,
                };

                bool isCancelled = false;
                string currentFunction = "";
                string currentFunctionArguments = "";
                await foreach (ChatResult result in api.Chat.StreamChatEnumerableAsync(chatRequest).ConfigureAwait(false))
                {
                    if (isCancelled)
                        return isCancelled;

                    if (result.Choices != null && result.Choices[0] != null && result.Choices[0].FinishReason == "function_call" && convertedMessages.Last().Role != ChatMessageRole.Function)
                    {
                        return await CallFunction(currentFunction, currentFunctionArguments, userId, convertedMessages, streamGetter);
                    }
                    else if (result.Choices != null && result.Choices[0].Delta.FunctionCall?.Arguments != null)
                    {
                        if (result.Choices[0].Delta.FunctionCall.Name != null)
                        {
                            currentFunction = result.Choices[0].Delta.FunctionCall.Name;
                        }
                        else
                        {
                            currentFunctionArguments += result.Choices[0].Delta.FunctionCall.Arguments;
                        }
                    }

                    if (isCancelled)
                        return isCancelled;

                    foreach (ChatChoice choice in result.Choices?.Where(choice => !string.IsNullOrWhiteSpace(choice.Delta?.Content)))
                    {
                        isCancelled = await streamGetter(choice.Delta.Content).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return false;
        }

        private async Task<string> GetSingleResponseMostWideContext(string model, string setting, string question, string data)
        {
            var conversation = api.Chat.CreateConversation(new ChatRequest
            {
                Model = model
            });

            conversation.AppendSystemMessage(setting);
            conversation.AppendUserInput(question);
            conversation.AppendUserInput(data);

            return await conversation.GetResponseFromChatbotAsync().ConfigureAwait(false);
        }

        public async Task<string> GetSingleResponseMostSmart(string setting, string question, string data)
        {
            return await GetSingleResponseMostWideContext(gptLast, setting, question, data).ConfigureAwait(false);
        }

        public async Task<string> GetSingleResponseMostWideContext(string setting, string question, string data)
        {
            return await GetSingleResponseMostWideContext(gptWideContext, question, setting, data).ConfigureAwait(false);
        }
    }
}