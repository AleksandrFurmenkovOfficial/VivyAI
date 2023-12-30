using System.Text.Json.Serialization;
using Newtonsoft.Json;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.AIFunctions
{
    internal sealed class DescribeImageAiFunction : AiFunctionBase
    {
        public override string Name => "DescribeImage";

        public override object Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function enables detailed image descriptions by leveraging another GPT-Vision AI.\n" +
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
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
        {
            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            string description = await api.GetImageDescription(new Uri(deserializedParameters.ImageURLToDescribe.Value),
                deserializedParameters.QuestionAboutImage.Value).ConfigureAwait(false);
            return new AiFunctionResult(description);
        }
    }
}