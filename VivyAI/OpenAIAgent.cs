using Newtonsoft.Json;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using System.Data;
using System.Diagnostics;
using System.Text.Json.Serialization;
using VivyAI.AIFunctions;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed partial class OpenAIAgent : IAIAgent
    {
        private readonly Dictionary<string, IAIFunction> functions;
        private static readonly ThreadLocal<Random> random = new(() => new Random(Guid.NewGuid().GetHashCode()));

        private const string gptModel = "gpt-4-1106-preview";

        private string aiName = "";
        public string AIName { get => aiName; set => aiName = value; }

        private string systemMessage = "";
        public string SystemMessage { get => systemMessage; set => systemMessage = value; }

        private bool enableFunctions;
        public bool EnableFunctions { get => enableFunctions; set => enableFunctions = value; }

        private readonly string chatId;
        private readonly string token;
        private readonly IOpenAi openAIAPI;

        public OpenAIAgent(string chatId, string token)
        {
            this.chatId = chatId;
            this.token = token;

            _ = OpenAiService.Instance.AddOpenAi(settings => { settings.ApiKey = token; }, "NoDI");
            this.openAIAPI = OpenAiService.Factory.Create("NoDI");

            functions = new Dictionary<string, IAIFunction>();
            RegisterAIFunctions();
        }

        private IOpenAi GetAPI()
        {
            return openAIAPI;
        }

        private void RegisterAIFunctions()
        {
            AddFunction(new DrawImageByDescriptionAIFunction());
            AddFunction(new DescribeImageAIFunction());

            AddFunction(new ReadVivyDiaryAIFunction());
            AddFunction(new RetrieveAnswerFromVivyMemoryAboutUserAIFunction());
            AddFunction(new WriteToVivyDiaryAIFunction());

            AddFunction(new ExtractInformationFromURLAIFunction());
            AddFunction(new AskMyselfAIFunction());
        }

        private void AddFunction(IAIFunction function)
        {
            functions.Add(function.Name, function);
        }

        private static ChatMessage CreateFunctionResultMessage(string functionName, AIFunctionResult result)
        {
            return new ChatMessage
            {
                Role = Strings.RoleFunction,
                Content = "{\"result\": " + JsonConvert.SerializeObject(result.text) + " }",
                Name = functionName,
                ImageUrl = result.imageUrl
            };
        }

        private async Task<bool> CallFunction(string functionName,
                                              string functionArguments,
                                              string userId,
                                              Func<ResponseStreamChunk, Task<bool>> streamGetter)
        {
            ChatMessage resultMessage;
            try
            {
                Debug.WriteLine($"Vivy calls function {functionName}({functionArguments})");
                var result = await functions[functionName].Call(this, functionArguments, userId).ConfigureAwait(false);
                resultMessage = CreateFunctionResultMessage(functionName, result);
            }
            catch (Exception e)
            {
                App.LogException(e);
                resultMessage = CreateFunctionResultMessage(functionName, new AIFunctionResult("Exception: Can't call function " + functionName + " (" + functionArguments + "); Possible issues:\n1. function name is incorrect\n2. wrong arguments are provided\n3. internal function error\nException message: " + e.Message));
            }

            return await streamGetter(new ResponseStreamChunk(resultMessage)).ConfigureAwait(false);
        }

        private static List<Rystem.OpenAi.Chat.ChatMessage> ConvertMessages(List<IChatMessage> messages)
        {
            return messages.Select(message => ConvertMessage(message)).ToList();
        }

        private static Rystem.OpenAi.Chat.ChatMessage ConvertMessage(IChatMessage message)
        {
            return new Rystem.OpenAi.Chat.ChatMessage
            {
                StringableRole = message.Role,
                Content = message.Content,
                Name = message.Name
            };
        }

        public async Task GetAIResponse(
            List<IChatMessage> messages,
            Func<ResponseStreamChunk, Task<bool>> streamGetter)
        {
            var convertedMessages = ConvertMessages(messages);
            await GetAIResponseImpl(convertedMessages, streamGetter, chatId).ConfigureAwait(false);
        }

        private void AddFunctions(ChatRequestBuilder builder)
        {
            foreach (var function in functions)
            {
                _ = builder.WithFunction((JsonFunction)function.Value.Description());
            }
        }

        private static double GetTemperature()
        {
            const double minTemperature = 0.3;
            const double oneThird = 1.0 / 3.0;
            return minTemperature + (random.Value.NextDouble() * oneThird);
        }

        private async Task GetAIResponseImpl(
            List<Rystem.OpenAi.Chat.ChatMessage> convertedMessages,
            Func<ResponseStreamChunk, Task<bool>> streamGetter,
            string userId)
        {
            bool isFunctionCall = false;
            bool isCancelled = false;
            try
            {
                var api = GetAPI();
                var messageBuilder = api.Chat.Request(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.System, Content = systemMessage })
                                             .WithModel(gptModel)
                                             .WithTemperature(GetTemperature());

                if (enableFunctions)
                {
                    AddFunctions(messageBuilder);
                }

                foreach (var message in convertedMessages)
                {
                    _ = messageBuilder.AddMessage(message);
                }

                string currentFunction = "";
                string currentFunctionArguments = "";
                await foreach (var x in messageBuilder.ExecuteAsStreamAsync())
                {
                    var newPartOfResponse = x.LastChunk?.Choices[0];
                    var messageDelta = newPartOfResponse?.Delta?.Content ?? "";
                    var functionDelta = newPartOfResponse?.Delta?.Function;

                    if (!string.IsNullOrEmpty(messageDelta))
                    {
                        isCancelled = await streamGetter(new ResponseStreamChunk(messageDelta)).ConfigureAwait(false);
                        if (isCancelled)
                        {
                            return;
                        }
                    }
                    else if (functionDelta != null)
                    {
                        if (functionDelta.Name != null)
                        {
                            currentFunction = functionDelta.Name;
                        }
                        else
                        {
                            currentFunctionArguments += functionDelta.Arguments;
                        }
                    }
                    else if (newPartOfResponse.FinishReason == "function_call")
                    {
                        isFunctionCall = true;
                        await CallFunction(currentFunction, currentFunctionArguments, userId, streamGetter).ConfigureAwait(false);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                App.LogException(e);
            }
            finally
            {
                if (!isFunctionCall && !isCancelled)
                {
                    await streamGetter(new ResponseStreamChunk()).ConfigureAwait(false);
                }
            }
        }

        private async Task<string> GetSingleResponse(string model, string setting, string question, string data)
        {
            var api = GetAPI();
            var request = api.Chat.Request(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.System, Content = setting })
                                  .WithModel(model)
                                  .WithTemperature(GetTemperature());


            if (!string.IsNullOrEmpty(data))
            {
                _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.User, Content = data });
            }

            _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.User, Content = question });
            return (await request.ExecuteAsync().ConfigureAwait(false)).Choices[0].Message.Content;
        }

        public async Task<string> GetSingleResponse(string setting, string question, string data)
        {
            return await GetSingleResponse(gptModel, setting, question, data).ConfigureAwait(false);
        }
    }
}