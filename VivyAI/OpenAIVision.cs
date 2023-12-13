using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace VivyAI
{
    internal sealed partial class OpenAI
    {
        private const string apiBase = "https://api.openai.com/v1";
        private const string getImageModel = "dall-e-3";
        private const string getImageDescriptionModel = "gpt-4-vision-preview";

        private HttpRequestMessage GetRequest(dynamic payload, string endpoint)
        {
            string jsonPayload = JsonConvert.SerializeObject(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        public async Task<Uri> GetImage(string imageDescription, string userId)
        {
            var payload = new
            {
                model = getImageModel,
                prompt = imageDescription,
                n = 1,
                size = "1792x1024",
                response_format = "url",
                user = userId
            };

            try
            {
                using var request = GetRequest(payload, $"{apiBase}/images/generations");
                dynamic response = await Utils.GetJsonResponse(request);
                string result = response?.data[0]?.url ?? string.Empty;
                return new Uri(result);
            }
            catch (Exception e)
            {
                App.LogException(e);
            }

            return null;
        }

        public async Task<string> GetImageDescription(Uri imageUrl, string question)
        {
            var payload = new
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
                            new { type = "image_url", image_url = new { url = $"data:image/jpeg;base64,{await Utils.EncodeImageToBase64(imageUrl)}" } }
                        }
                    }
                },
                max_tokens = 512
            };

            try
            {
                using var request = GetRequest(payload, $"{apiBase}/chat/completions");
                dynamic response = await Utils.GetJsonResponse(request);
                string result = response?.choices[0]?.message?.content ?? string.Empty;
                return result;
            }
            catch (Exception e)
            {
                App.LogException(e);
            }

            return string.Empty;
        }
    }
}