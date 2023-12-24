using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class ExtractInformationFromURLAIFunction : AIFunctionBase
    {
        public override string Name => "ExtractInformationFromURL";

        public override object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function retrieves and analyzes the content of a specified webpage to extract relevant information based on a provided question.\n" +
                          "Vivy's rating for the function: 8 out of 10.",

            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("Url", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "The URL of the webpage to be analyzed.\n" +
                                  "Prefer simple web pages, ideally with plain text data, over complex URLs loaded with scripts and other elements."
                })
                .AddRequired("Url")
                .AddPrimitive("Question", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "A question regarding the information to be extracted from the webpage."
                })
                .AddRequired("Question")
        };

        private static async Task<string> GetTextContentOnly(Uri url)
        {
            using (var httpClient = new HttpClient())
            {
                string responseBody = await httpClient.GetStringAsync(url).ConfigureAwait(false);
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(responseBody);
                return htmlDocument.DocumentNode.InnerText;
            }
        }

        public override async Task<AIFunctionResult> Call(IAIAgent api, string parameters, string userId)
        {
            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            string textContent = await GetTextContentOnly(new Uri(deserializedParameters.Url.Value)).ConfigureAwait(false);
            return new AIFunctionResult(await api.GetSingleResponse("I am tasked with extracting facts from the given text.", deserializedParameters.Question.Value, textContent));
        }
    }
}