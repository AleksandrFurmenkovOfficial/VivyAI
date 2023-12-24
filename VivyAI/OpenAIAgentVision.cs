using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace VivyAI
{
    internal sealed partial class OpenAIAgent
    {
        private const string apiBase = "https://api.openai.com/v1";
        private const string getImageModel = "dall-e-3";
        private const string getImageDescriptionModel = "gpt-4-vision-preview";

        private async Task<object> DoRequest(object payload, string endpoint)
        {
            HttpRequestMessage CreateRequest(object payload, string endpoint)
            {
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return request;
            }

            using (var request = CreateRequest(payload, endpoint))
            {
                return await Utils.GetJsonResponse(request).ConfigureAwait(false);
            }
        }

        public async Task<Uri> GetImage(string imageDescription, string userId)
        {
            dynamic response = await DoRequest(new
            {
                model = getImageModel,
                prompt = imageDescription,
                n = 1,
                size = "1792x1024",
                response_format = "url",
                user = userId
            }, $"{apiBase}/images/generations").ConfigureAwait(false);

            return new Uri((string)response.data[0].url);
        }

        public async Task<string> GetImageDescription(Uri imageUrl, string question)
        {
            dynamic response = await DoRequest(new
            {
                model = getImageDescriptionModel,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = new object[]
                        {
                            new { type = "text", text = systemMessage }
                        }
                    },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = string.IsNullOrEmpty(question) ? question : Strings.WhatIsOnTheImage },
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{await Utils.EncodeImageToBase64(imageUrl).ConfigureAwait(false)}" } }
                        }
                    }
                },
                max_tokens = 512
            }, $"{apiBase}/chat/completions").ConfigureAwait(false);
            return response.choices[0].message.content;
        }
    }
}