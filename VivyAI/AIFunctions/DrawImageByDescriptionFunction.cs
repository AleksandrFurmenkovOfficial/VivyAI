using Newtonsoft.Json;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class DrawImageByDescriptionFunction : IFunction
    {
        internal sealed class ImageDetailedDescriptionModel
        {
            [JsonPropertyName("ImageToDrawDetailedDescription")]
            public string ImageToDrawDetailedDescription { get; set; }
        }

        public string Name => "DrawImageByDescription";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function allows you to draw an image by providing detailed text description for what you want to get(How much Vivy like the function: 9/10).",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("ImageToDrawDetailedDescription", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "Detailed description of the picture you want to draw."
                })
                .AddRequired("ImageToDrawDetailedDescription")
        };

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            var model = JsonConvert.DeserializeObject<ImageDetailedDescriptionModel>(parameters);
            var imageDetailedDescription = model.ImageToDrawDetailedDescription;
            var image = await api.GetImage(imageDetailedDescription, userId);
            if (image == null)
            {
                return new FuncResult($"The image hasn't been created due to an internal error. Please report the issue to the user and ask if they would like to try again.");
            }

            return new FuncResult($"The image by description \"{imageDetailedDescription}\"\n has been created and the user is already viewing it. Now you should briefly describe for a user what you have created.", image);
        }
    }
}