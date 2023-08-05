using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class ExtractInformationFromURLFunction : IFunction
    {
        internal sealed class ExtractInformationFromURLModel
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
            [JsonPropertyName("question")]
            public string Question { get; set; }
        }

        private static readonly HttpClient httpClient = new();

        public string name => "ExtractInformationFromURL";

        public object Description()
        {
            return new JsonFunction
            {
                Name = name,
                Description = "This function retrieves and analyzes the content of a specified webpage to extract the required information based on the provided question.",
                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("url", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "The URL of the webpage to be analyzed."
                    })
                    .AddRequired("url")
                    .AddPrimitive("question", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "The question about the information to be extracted from the webpage."
                    })
                    .AddRequired("question")
            };
        }

        private static async Task<string> GetTextContentOnly(string url)
        {
            string responseBody = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(responseBody);
            return htmlDocument.DocumentNode.InnerText;
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            ExtractInformationFromURLModel model = JsonConvert.DeserializeObject<ExtractInformationFromURLModel>(parameters);
            string textContent = await GetTextContentOnly(model.Url).ConfigureAwait(false);
            return await api.GetSingleResponseMostWideContext("You are a fact extractor from given text.", model.Question, textContent).ConfigureAwait(false);
        }
    }
}