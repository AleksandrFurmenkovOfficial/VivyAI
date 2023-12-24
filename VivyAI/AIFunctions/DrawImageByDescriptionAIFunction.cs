using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class DrawImageByDescriptionAIFunction : AIFunctionBase
    {
        public override string Name => "DrawImageByDescription";

        public override object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function enables the creation of an image based on a detailed text description provided by the user.\n" +
                          "Vivy's rating for the function: 9.5 out of 10.",

            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("DetailedDescriptionToDrawImage", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "A detailed description of the image you wish to create."
                })
                .AddRequired("DetailedDescriptionToDrawImage")
        };

        public override async Task<AIFunctionResult> Call(IAIAgent api, string parameters, string userId)
        {
            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            var imageDetailedDescription = deserializedParameters.DetailedDescriptionToDrawImage.Value;
            Uri image = await api.GetImage(imageDetailedDescription, userId);
            if (image == null)
            {
                return new AIFunctionResult(
                    "An internal error occurred, and the image could not be created.\n" +
                    "Please report this issue and inquire if the user would like to try again.");
            }

            return new AIFunctionResult(
                $"The image has been successfully created. " +
                $"The user is currently viewing it. " +
                $"Now, you should briefly describe to the user what has been created.\n" +
                $"Url to the image: {image}", image);
        }
    }
}