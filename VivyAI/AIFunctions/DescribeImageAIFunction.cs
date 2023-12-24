using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class DescribeImageAIFunction : AIFunctionBase
    {
        public override string Name => "DescribeImage";

        public override object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function enables detailed image descriptions by leveraging another GPT-Vision AI.\n" +
                          "Vivy's rating for the function: 9 out of 10.",

            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("ImageURLToDescribe", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "A full path URL of the image for which a detailed description is sought."
                })
                .AddRequired("ImageURLToDescribe")
                .AddPrimitive("QuestionAboutImage", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "An additional question regarding the image's content."
                })
                .AddRequired("QuestionAboutImage")
        };

        public override async Task<AIFunctionResult> Call(IAIAgent api, string parameters, string userId)
        {
            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            string description = await api.GetImageDescription(new Uri(deserializedParameters.ImageURLToDescribe.Value), deserializedParameters.QuestionAboutImage.Value);
            return new AIFunctionResult($"{description}");
        }
    }
}