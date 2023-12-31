using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace VivyAI.Implementation
{
    internal sealed partial class OpenAiAgent
    {
        private const string ApiBase = "https://api.openai.com/v1";
        private const string GetImageModel = "dall-e-3";
        private const string GetImageDescriptionModel = "gpt-4-vision-preview";

        public async Task<Uri> GetImage(string imageDescription, string userId)
        {
            dynamic response = await DoRequest(new
                {
                    model = GetImageModel,
                    prompt = imageDescription,
                    n = 1,
                    size = "1792x1024",
                    response_format = "url",
                    user = userId
                }, $"{ApiBase}/images/generations").ConfigureAwait(false);

            return new Uri((string)response.data[0].url);
        }

        public async Task<string> GetImageDescription(Uri imageUrl, string question)
        {
            dynamic response = await DoRequest(new
                {
                    model = GetImageDescriptionModel,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = new object[]
                            {
                                new { type = "text", text = SystemMessage }
                            }
                        },
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = string.IsNullOrEmpty(question) ? question : Strings.WhatIsOnTheImage
                                },
                                new
                                {
                                    type = "image_url",
                                    image_url = new
                                    {
                                        url =
                                            $"data:image/jpeg;base64,{await Utils.EncodeImageToBase64(imageUrl).ConfigureAwait(false)}"
                                    }
                                }
                            }
                        }
                    },
                    max_tokens = 512
                }, $"{ApiBase}/chat/completions").ConfigureAwait(false);
            return response.choices[0].message.content;
        }

        private async Task<object> DoRequest(object payload, string endpoint)
        {
            HttpRequestMessage CreateRequest()
            {
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json") };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return request;
            }

            using (var request = CreateRequest())
            {
                return await Utils.GetJsonResponse(request).ConfigureAwait(false);
            }
        }
    }
}