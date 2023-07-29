using Newtonsoft.Json.Linq;
using OpenAI_API.ChatFunctions;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class ExtractDataFromTextFuncttion : IFunction
    {
        public string name => "ExtractDataFromText";

        public object Description()
        {
            var parameters = new JObject()
            {
                ["type"] = "object",
                ["required"] = new JArray("role", "question", "text"),
                ["properties"] = new JObject
                {
                    ["role"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Indicates from which point of view to answer the question on the text."
                    },
                    ["question"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Question on the text."
                    },
                    ["text"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The text for which questions are asked."
                    }
                }
            };

            string functionDescription = "This function allows you to extract answers from large text.";
            return new Function(name, functionDescription, parameters);
        }

        public Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            return api.GetSingleResponseMostWideContext((string)parameters.role, (string)parameters.question, (string)parameters.data);
        }
    }
}