using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class ExtractDataFromTextFuncttion : IFunction
    {
        internal sealed class ExtractDataFromTextModel
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }
            [JsonPropertyName("question")]
            public string Question { get; set; }
            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        public string name => "ExtractDataFromText";

        public object Description()
        {
            return new JsonFunction
            {
                Name = name,
                Description = "This function allows you to extract answers from large text.",
                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("role", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "Indicates from which point of view to answer the question on the text."
                    })
                    .AddRequired("role")
                    .AddPrimitive("question", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "Question on the text."
                    })
                    .AddRequired("question")
                    .AddPrimitive("text", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "The text for which questions are asked."
                    })
                    .AddRequired("text")
            };
        }

        public Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            ExtractDataFromTextModel model = JsonConvert.DeserializeObject<ExtractDataFromTextModel>(parameters);
            return api.GetSingleResponseMostWideContext(model.Role, model.Question, model.Text);
        }
    }
}