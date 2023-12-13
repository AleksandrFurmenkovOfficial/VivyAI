using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class DescribeImageFunction : IFunction
    {
        internal sealed class ImageDetailedDescriptionRequestModel
        {
            [JsonPropertyName("ImageURLToDescribe")]
            public string ImageURLToDescribe { get; set; }

            [JsonPropertyName("QuestionAboutImage")]
            public string QuestionAboutImage { get; set; }
        }

        public string Name => "DescribeImage";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "The function allows you to get detailed image description by another GPT-Vision AI(How much Vivy like the function: 10/10).",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("ImageURLToDescribe", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "URL for an image for which you want to get detailed description."
                })
                .AddRequired("ImageURLToDescribe")
                .AddPrimitive("QuestionAboutImage", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "Additional question about an image content."
                })
                .AddRequired("QuestionAboutImage")
        };

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            var model = JsonConvert.DeserializeObject<ImageDetailedDescriptionRequestModel>(parameters);
            return new FuncResult(await api.GetImageDescription(new Uri(model.ImageURLToDescribe), model.QuestionAboutImage));
        }
    }
}