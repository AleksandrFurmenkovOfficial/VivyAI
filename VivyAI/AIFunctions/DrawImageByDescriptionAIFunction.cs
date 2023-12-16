using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class DrawImageByDescriptionAIFunction : IAIFunction
    {
        public string Name => "DrawImageByDescription";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function enables the creation of an image based on a detailed text description provided by the user.\nVivy's rating for the function: 9.5 out of 10.",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("ImageToDrawDetailedDescription", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "A detailed description of the image you wish to create."
                })
                .AddRequired("ImageToDrawDetailedDescription")
        };

        public async Task<AIFunctionResult> Call(IAIAgent api, dynamic parameters, string userId)
        {
            var deserializedParameters = JsonConvert.DeserializeObject(parameters);
            var imageDetailedDescription = deserializedParameters.ImageToDrawDetailedDescription.Value;
            var image = await api.GetImage(imageDetailedDescription, userId);
            if (image == null)
            {
                return new AIFunctionResult("An internal error occurred, and the image could not be created. Please report this issue and inquire if the user would like to try again.");
            }

            return new AIFunctionResult($"The image described as \"{imageDetailedDescription}\" has been successfully created. The user is currently viewing it. Now, you should briefly describe to the user what has been created.", image);
        }
    }
}