using System.Text.Json.Serialization;
using HtmlAgilityPack;
using Newtonsoft.Json;
using VivyAi.Interfaces;

namespace VivyAi.Implementation.AiFunctions
{
    internal sealed class ExtractInformationFromUrlAiFunction : AiFunctionBase
    {
        public override string Name => "ExtractInformationFromUrl";

        public override JsonFunction Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function retrieves and analyzes the content of a specified webpage to extract relevant information based on a provided question.\n" +
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
        }

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

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
        {
            var deserializedParameters =
                JsonConvert.DeserializeObject<ExtractInformationFromUrlRequest>(parameters);
            string textContent =
                await GetTextContentOnly(new Uri(deserializedParameters.Url)).ConfigureAwait(false);
            return new AiFunctionResult(await api.GetResponse(
                "I am tasked with extracting facts from the given text.", deserializedParameters.Question,
                textContent).ConfigureAwait(false));
        }

        private sealed class ExtractInformationFromUrlRequest(string url, string question)
        {
            [JsonProperty] public string Url { get; } = url;

            [JsonProperty] public string Question { get; } = question;
        }
    }
}