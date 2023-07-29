using Newtonsoft.Json.Linq;
using OpenAI_API.ChatFunctions;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class RetrieveAnswerFromVivyMemoryFunction : IFunction
    {
        public string name => "RetrieveAnswerFromVivyMemory";

        public object Description()
        {
            var parameters = new JObject()
            {
                ["type"] = "object",
                ["required"] = new JArray("question"),
                ["properties"] = new JObject
                {
                    ["question"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "A question (about recent impressions, facts and events) to my personal memory. If I know the answer, I will recall it."
                    }
                }
            };

            string functionDescription = "This function allows Vivy to recall recent data, impressions, facts, and events from memory.";
            return new Function(name, functionDescription, parameters);
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = $"{userId}.txt";
            if (!File.Exists(path))
            {
                return "No information available.";
            }

            dynamic newParameters = new ExpandoObject();
            newParameters.role = "You are the analyzer of memories";
            newParameters.question = parameters.question;
            newParameters.data = await File.ReadAllTextAsync(path);
            return await new ExtractDataFromTextFuncttion().Call(api, newParameters, userId);
        }
    }
}