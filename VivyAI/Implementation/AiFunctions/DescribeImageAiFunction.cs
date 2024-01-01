using System.Text.Json.Serialization;
using Newtonsoft.Json;
using VivyAi.Interfaces;

namespace VivyAi.Implementation.AiFunctions
{
    internal sealed class DescribeImageAiFunction : AiFunctionBase
    {
        public override string Name => "DescribeImage";

        public override JsonFunction Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function enables detailed image descriptions by leveraging another GPT-Vision Ai.\n" +
                    "Vivy's rating for the function: 9 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("ImageUrlToDescribe", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A full path URL of the image for which a detailed description is sought."
                    })
                    .AddRequired("ImageUrlToDescribe")
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
            var deserializedParameters =
                JsonConvert.DeserializeObject<DescribeImageRequest>(parameters);
            string description = await api.GetImageDescription(new Uri(deserializedParameters.ImageUrlToDescribe),
                deserializedParameters.QuestionAboutImage).ConfigureAwait(false);
            return new AiFunctionResult(description);
        }

        private sealed class DescribeImageRequest(string imageUrlToDescribe, string questionAboutImage)
        {
            [JsonProperty] public string ImageUrlToDescribe { get; } = imageUrlToDescribe;

            [JsonProperty] public string QuestionAboutImage { get; } = questionAboutImage;
        }
    }
}