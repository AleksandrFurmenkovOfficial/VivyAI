using System.Text.Json.Serialization;
using Newtonsoft.Json;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.AIFunctions
{
    internal sealed class DrawImageByDescriptionAiFunction : AiFunctionBase
    {
        public override string Name => "DrawImageByDescription";

        public override object Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description =
                    "This function enables the creation of an image based on a detailed text description provided by the user.\n" +
                    "Vivy's rating for the function: 9.5 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("DetailedDescriptionToDrawImage", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "A detailed English description of the image you wish to create."
                    })
                    .AddRequired("DetailedDescriptionToDrawImage")
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
        {
            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            var imageDetailedDescription = deserializedParameters.DetailedDescriptionToDrawImage.Value;
            Uri image = await api.GetImage(imageDetailedDescription, userId).ConfigureAwait(false);
            if (image == null)
            {
                return new AiFunctionResult(
                    "An internal error occurred, and the image could not be created.\n" +
                    "Please report this issue and inquire if the user would like to try again.");
            }

            return new AiFunctionResult(
                $"The image has been successfully created. " +
                $"The user is currently viewing it. " +
                $"Now, you should briefly describe to the user what has been created.\n" +
                $"Url to the image: {image}",
                image);
        }
    }
}