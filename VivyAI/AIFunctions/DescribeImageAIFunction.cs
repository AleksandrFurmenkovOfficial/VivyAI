using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class DescribeImageAIFunction : IAIFunction
    {
        public string Name => "DescribeImage";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function enables detailed image descriptions by leveraging another GPT-Vision AI.\nVivy's rating for the function: 9 out of 10.",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("ImageURLToDescribe", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "URL of the image for which a detailed description is sought."
                })
                .AddRequired("ImageURLToDescribe")
                .AddPrimitive("QuestionAboutImage", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "An additional question regarding the image's content."
                })
                .AddRequired("QuestionAboutImage")
        };

        public async Task<AIFunctionResult> Call(IAIAgent api, dynamic parameters, string userId)
        {
            var deserializedParameters = JsonConvert.DeserializeObject(parameters);
            return new AIFunctionResult(await api.GetImageDescription(new Uri(deserializedParameters.ImageURLToDescribe.Value), deserializedParameters.QuestionAboutImage.Value));
        }
    }
}