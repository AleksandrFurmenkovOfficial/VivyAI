using System.Text.Json.Serialization;
using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal sealed class OpenAiImagePainter(string token) : IAiImagePainter
    {
        private const string ApiHost = "https://api.openai.com/v1";
        private const string GetImageModel = "dall-e-3";

        public async Task<Uri> GetImage(string imageDescription, string userId)
        {
            var response = await Utils.DoRequest<ImageResult>(new
                {
                    model = GetImageModel,
                    prompt = imageDescription,
                    n = 1,
                    size = "1792x1024",
                    response_format = "url",
                    user = userId
                }, $"{ApiHost}/images/generations", token).ConfigureAwait(false);

            string uriString = response?.Data?[0].Url;
            return uriString != null ? new Uri(uriString) : null;
        }

        private sealed class ImageResult(List<ImageData> data)
        {
            [JsonPropertyName("created")] public long Created { get; set; }

            [JsonPropertyName("data")] public List<ImageData> Data { get; } = data;
        }

        private sealed class ImageData(string url)
        {
            [JsonPropertyName("revised_prompt")] public string RevisedPrompt { get; set; }

            [JsonPropertyName("url")] public string Url { get; } = url;
        }
    }
}