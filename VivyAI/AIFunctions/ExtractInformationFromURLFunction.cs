using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class ExtractInformationFromURLFunction : IFunction
    {
        internal sealed class ExtractInformationFromURLModel
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
            [JsonPropertyName("question")]
            public string Question { get; set; }
        }

        private static readonly HttpClient httpClient = new();

        public string Name => "ExtractInformationFromURL";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function retrieves and analyzes the content of a specified webpage to extract the required information based on the provided question(How much Vivy like the function: 6/10).",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("url", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "The URL of the webpage to be analyzed. Use simple web pages (if it possible only with plain text data!) over full URLs loaded with scripts, etc."
                })
                .AddRequired("url")
                .AddPrimitive("question", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "The question about the information to be extracted from the webpage."
                })
                .AddRequired("question")
        };

        private static async Task<string> GetTextContentOnly(Uri url)
        {
            string responseBody = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(responseBody);
            return htmlDocument.DocumentNode.InnerText;
        }

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            var model = JsonConvert.DeserializeObject<ExtractInformationFromURLModel>(parameters);
            string textContent = await GetTextContentOnly(new Uri(model.Url));
            return new FuncResult(await api.GetSingleResponse("You are a fact extractor from given text.", model.Question, textContent));
        }
    }
}