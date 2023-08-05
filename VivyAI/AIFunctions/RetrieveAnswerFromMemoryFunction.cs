using Newtonsoft.Json;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VivyAI.Interfaces;
using static VivyAI.Functions.ExtractDataFromTextFuncttion;

namespace VivyAI.Functions
{
    internal class RetrieveAnswerFromVivyMemoryFunction : IFunction
    {
        internal sealed class RetrieveAnswerFromVivyMemoryModel
        {
            [JsonPropertyName("question")]
            public string Question { get; set; }
        }

        public string name => "RetrieveAnswerFromVivyMemory";

        public object Description()
        {
            return new JsonFunction
            {
                Name = name,
                Description = "This function allows Vivy to recall recent data, impressions, facts, and events from memory.",
                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("question", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A question (about recent impressions, facts and events) to my personal memory. If I know the answer, I will recall it."
                    })
                    .AddRequired("question")
            };
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = $"{userId}.txt";
            if (!File.Exists(path))
            {
                return "There are no records in the diary about user.";
            }

            var queryToMemory = new ExtractDataFromTextModel();
            queryToMemory.Role = "You are a fact extractor from given text.";
            queryToMemory.Question = JsonConvert.DeserializeObject<RetrieveAnswerFromVivyMemoryModel>(parameters).Question;
            queryToMemory.Text = await File.ReadAllTextAsync(path);
            return await new ExtractDataFromTextFuncttion().Call(api, JsonConvert.SerializeObject(queryToMemory), userId);
        }
    }
}