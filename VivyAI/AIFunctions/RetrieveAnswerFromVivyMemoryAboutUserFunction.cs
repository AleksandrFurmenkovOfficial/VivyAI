using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.AIFunctions;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal sealed class RetrieveAnswerFromVivyMemoryAboutUserFunction : UserFileFunctionBase, IFunction
    {
        internal sealed class QuestionToVivyMemoryAboutUserModel
        {
            [JsonPropertyName("question")]
            public string Question { get; set; }
        }

        public string Name => "RetrieveAnswerFromVivyMemoryAboutUser";

        public object Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description = "This function allows Vivy to recall recent data, impressions, facts, and events about user from memory(How much Vivy like the function: 8/10).",
                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("question", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A question (about recent impressions, facts and events) to my personal memory about user. If I know the answer, I will recall it."
                    })
                    .AddRequired("question")
            };
        }

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = GetPathToUserAssociatedMemories(api.AIName, userId);
            if (!File.Exists(path))
            {
                return new FuncResult("There are no records in the long-term memory about the user.");
            }

            return new FuncResult(await api.GetSingleResponse("You are a fact extractor from given text.",
                JsonConvert.DeserializeObject<QuestionToVivyMemoryAboutUserModel>(parameters).Question,
                await File.ReadAllTextAsync(path)));
        }
    }
}