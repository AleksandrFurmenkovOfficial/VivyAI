using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OpenAI_API.ChatFunctions;
using System.Net.Http;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class ExtractInformationFromURLFunction : IFunction
    {
        private static readonly HttpClient httpClient = new();

        public string name => "ExtractInformationFromURL";

        public object Description()
        {
            var parameters = new JObject()
            {
                ["type"] = "object",
                ["required"] = new JArray("url", "question"),
                ["properties"] = new JObject
                {
                    ["url"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The URL of the webpage to be analyzed."
                    },
                    ["question"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The question about the information to be extracted from the webpage."
                    }
                }
            };

            string functionDescription = "This function retrieves and analyzes the content of a specified webpage to extract the required information based on the provided question.";
            return new Function(name, functionDescription, parameters);
        }

        private static async Task<string> GetTextContentOnly(dynamic parameters)
        {
            string responseBody = await httpClient.GetStringAsync((string)parameters.url).ConfigureAwait(false);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(responseBody);
            return htmlDocument.DocumentNode.InnerText;
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string textContent = await GetTextContentOnly(parameters).ConfigureAwait(false);
            return await api.GetSingleResponseMostWideContext("You are a knowledge extractor from a web page", (string)parameters.question, textContent).ConfigureAwait(false);
        }
    }
}