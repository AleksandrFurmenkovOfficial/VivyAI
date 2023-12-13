using Newtonsoft.Json;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using System.Data;
using System.Text.Json.Serialization;
using VivyAI.AIFunctions;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed partial class OpenAI : IOpenAI
    {
        private readonly Dictionary<string, IFunction> functions;
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

        public OpenAI(string chatId, string token)
        {
            this.chatId = chatId;
            this.token = token;



            functions = new Dictionary<string, IFunction>();
            RegisterAIFunctions();
        }

        private IOpenAiChat API()
        {
            _ = OpenAiService.Instance.AddOpenAi(settings => { settings.ApiKey = token; }, "NoDI");
            return OpenAiService.Factory.Create("NoDI").Chat;
        }

        private void RegisterAIFunctions()
        {
            AddFunction(new DrawImageByDescriptionFunction());
            AddFunction(new DescribeImageFunction());

            AddFunction(new ReadVivyDiaryFunction());
            AddFunction(new RetrieveAnswerFromVivyMemoryAboutUserFunction());
            AddFunction(new WriteToVivyDiaryFunction());

            AddFunction(new ExtractInformationFromURLFunction());
            AddFunction(new AskMyselfFunction());
        }

        private void AddFunction(IFunction function)
        {
            functions.Add(function.Name, function);
        }

        private static ChatMessage CreateFunctionResultMessage(string functionName, FuncResult result)
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
                                              Func<AnswerStreamStep, Task<bool>> streamGetter)
        {
            ChatMessage resultMessage;
            try
            {
                Console.WriteLine($"Vivy calls function {functionName}({functionArguments})");
                var result = await functions[functionName].Call(this, functionArguments, userId);
                resultMessage = CreateFunctionResultMessage(functionName, result);
            }
            catch (Exception e)
            {
                App.LogException(e);
                resultMessage = CreateFunctionResultMessage(functionName, new FuncResult("Exception: Can't call function " + functionName + " (" + functionArguments + "); Possible issues:\n1. function name is incorrect\n2. wrong arguments are provided\n3. internal function error\nException message: " + e.Message));
            }

            return await streamGetter(new AnswerStreamStep(resultMessage));
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

        public async Task<bool> GetAIResponse(List<IChatMessage> messages, Func<AnswerStreamStep, Task<bool>> streamGetter)
        {
            var convertedMessages = ConvertMessages(messages);
            return await GetAIResponseImpl(convertedMessages, streamGetter, chatId);
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
            const double minTemperature = 0.2;
            const double oneThird = 1.0 / 3.0;
            return minTemperature + (random.Value.NextDouble() * oneThird);
        }

        private async Task<bool> GetAIResponseImpl(List<Rystem.OpenAi.Chat.ChatMessage> convertedMessages, Func<AnswerStreamStep, Task<bool>> streamGetter, string userId)
        {
            bool isCancelled = false;

            try
            {
                var messageBuilder = API().Request(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.System, Content = systemMessage })
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
                    if (isCancelled)
                    {
                        return isCancelled;
                    }

                    var newPartOfResponse = x.LastChunk?.Choices[0];
                    var messageDelta = newPartOfResponse?.Delta?.Content ?? "";
                    var functionDelta = newPartOfResponse?.Delta?.Function;

                    if (!string.IsNullOrEmpty(messageDelta))
                    {
                        isCancelled = await streamGetter(new AnswerStreamStep(messageDelta));
                        if (isCancelled)
                        {
                            return isCancelled;
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
                        return await CallFunction(currentFunction, currentFunctionArguments, userId, streamGetter);
                    }
                }
            }
            catch (Exception e)
            {
                App.LogException(e);
            }

            return isCancelled;
        }

        private async Task<string> GetSingleResponse(string model, string setting, string question, string data)
        {
            var request = API().Request(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.System, Content = setting })
                               .WithModel(model)
                               .WithTemperature(GetTemperature());


            if (!string.IsNullOrEmpty(data))
            {
                _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.User, Content = data });
            }

            _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage { Role = ChatRole.User, Content = question });
            return (await request.ExecuteAsync()).Choices[0].Message.Content;
        }

        public async Task<string> GetSingleResponse(string setting, string question, string data)
        {
            return await GetSingleResponse(gptModel, setting, question, data);
        }
    }
}