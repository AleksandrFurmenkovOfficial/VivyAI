using System.Diagnostics;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Rystem.OpenAi;
using Rystem.OpenAi.Chat;
using VivyAI.Implementation.AIFunctions;
using VivyAI.Interfaces;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace VivyAI.Implementation
{
    internal sealed partial class OpenAiAgent : IAiAgent
    {
        private const string GptModel = "gpt-4-1106-preview";
        private static readonly ThreadLocal<Random> Random = new(() => new Random(Guid.NewGuid().GetHashCode()));
        private readonly IDictionary<string, IAiFunction> functions;
        private readonly IOpenAi openAiApi;

        private readonly string token;

        public OpenAiAgent(string token)
        {
            this.token = token;

            const string noDi = "NoDI";
            _ = OpenAiService.Instance.AddOpenAi(settings => { settings.ApiKey = token; }, noDi);
            openAiApi = OpenAiService.Factory.Create(noDi);

            functions = GetAiFunctions();
        }

        public string AiName { get; set; } = "";
        public string SystemMessage { get; set; } = "";
        public bool EnableFunctions { get; set; }

        public Task GetAiResponse(
            string chatId,
            IEnumerable<IChatMessage> messages,
            Func<ResponseStreamChunk, Task<bool>> streamGetter)
        {
            return GetAiResponseImpl(ConvertMessages(messages), streamGetter, chatId);
        }

        public Task<string> GetSingleResponse(string setting, string question, string data)
        {
            return GetSingleResponse(GptModel, setting, question, data);
        }

        private IOpenAi GetApi()
        {
            return openAiApi;
        }

        private static IDictionary<string, IAiFunction> GetAiFunctions()
        {
            var functions = new Dictionary<string, IAiFunction>();

            void AddFunction(IAiFunction function)
            {
                functions.Add(function.Name, function);
            }

            AddFunction(new DrawImageByDescriptionAiFunction());
            AddFunction(new DescribeImageAiFunction());

            AddFunction(new ReadVivyDiaryAiFunction());
            AddFunction(new RetrieveAnswerFromVivyMemoryAboutUserAiFunction());
            AddFunction(new WriteToVivyDiaryAiFunction());

            AddFunction(new ExtractInformationFromUrlAiFunction());

            return functions;
        }

        private static string CallInfo(string function, string parameters)
        {
            return $"{{\"hidden_note\": I called function \"{function}\" with arguments \"{parameters}\".}}";
        }

        private IEnumerable<ChatMessage> CreateFunctionResultMessages(string functionName, string parameters,
            AiFunctionResult result)
        {
            var callMessage = new ChatMessage
            {
                Role = Strings.RoleAssistant,
                Content = CallInfo(functionName, parameters),
                Name = AiName
            };

            var resultMessage = new ChatMessage
            {
                Role = Strings.RoleFunction,
                Content = $"{{\"result\": {JsonConvert.SerializeObject(result.Result)}}}",
                Name = functionName,
                ImageUrl = result.ImageUrl
            };

            return new List<ChatMessage> { callMessage, resultMessage };
        }

        private async Task CallFunction(string functionName,
            string functionArguments,
            string userId,
            Func<ResponseStreamChunk, Task<bool>> streamGetter)
        {
            var resultMessages = new List<IChatMessage>();
            try
            {
                Debug.WriteLine($"Vivy calls function {functionName}({functionArguments})");
                var result = await functions[functionName].Call(this, functionArguments, userId).ConfigureAwait(false);
                resultMessages.AddRange(CreateFunctionResultMessages(functionName, functionArguments, result));
            }
            catch (Exception e)
            {
                resultMessages.AddRange(CreateFunctionResultMessages(functionName, functionArguments,
                    new AiFunctionResult("Exception: Can't call function " + functionName + " (" + functionArguments +
                                         "); Possible issues:\n1. function Name is incorrect\n2. wrong arguments are provided\n3. internal function error\nException message: " +
                                         e.Message)));
            }

            await streamGetter(new ResponseStreamChunk(resultMessages)).ConfigureAwait(false);
        }

        private static IEnumerable<Rystem.OpenAi.Chat.ChatMessage> ConvertMessages(IEnumerable<IChatMessage> messages)
        {
            return messages.Select(message => new Rystem.OpenAi.Chat.ChatMessage
            {
                StringableRole = message.Role,
                Content = message.Content,
                Name = message.Name
            });
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
            return minTemperature + (Random.Value.NextDouble() * oneThird);
        }

        private async Task GetAiResponseImpl(
            IEnumerable<Rystem.OpenAi.Chat.ChatMessage> convertedMessages,
            Func<ResponseStreamChunk, Task<bool>> streamGetter,
            string userId)
        {
            bool isFunctionCall = false;
            bool isCancelled = false;
            try
            {
                var api = GetApi();
                var messageBuilder = api.Chat.Request(new Rystem.OpenAi.Chat.ChatMessage
                        { Role = ChatRole.System, Content = SystemMessage })
                    .WithModel(GptModel)
                    .WithTemperature(GetTemperature());

                if (EnableFunctions)
                {
                    AddFunctions(messageBuilder);
                }

                foreach (var message in convertedMessages)
                {
                    _ = messageBuilder.AddMessage(message);
                }

                string currentFunction = "";
                string currentFunctionArguments = "";
                const string functionCallReason = "function_call";
                await foreach (var x in messageBuilder.ExecuteAsStreamAsync())
                {
                    var newPartOfResponse = x.LastChunk.Choices?.ElementAt(0);
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
                    else if (newPartOfResponse?.FinishReason == functionCallReason)
                    {
                        isFunctionCall = true;
                        await CallFunction(currentFunction, currentFunctionArguments, userId, streamGetter)
                            .ConfigureAwait(false);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                ExceptionHandler.LogException(e);
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
            var api = GetApi();
            var request = api.Chat.Request(new Rystem.OpenAi.Chat.ChatMessage
                    { Role = ChatRole.System, Content = setting })
                .WithModel(model)
                .WithTemperature(GetTemperature());


            if (!string.IsNullOrEmpty(data))
            {
                _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage
                    { Role = ChatRole.User, Content = $"{Strings.Text}:\n{data}" });
            }

            _ = request.AddMessage(new Rystem.OpenAi.Chat.ChatMessage
                { Role = ChatRole.User, Content = $"{Strings.Question}:\n{question}" });
            return (await request.ExecuteAsync().ConfigureAwait(false)).Choices[0].Message.Content;
        }
    }
}