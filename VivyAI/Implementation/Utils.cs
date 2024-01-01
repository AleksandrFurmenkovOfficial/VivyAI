using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace VivyAi.Implementation
{
    internal static class Utils
    {
        public static async Task<Stream> GetStreamFromUrlAsync(Uri url)
        {
            using (var httpClient = new HttpClient())
            {
                var bytes = await httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
                return new MemoryStream(bytes);
            }
        }

        public static async Task<string> EncodeImageToBase64(Uri imageUrl)
        {
            using (var httpClient = new HttpClient())
            {
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
                return Convert.ToBase64String(imageBytes);
            }
        }

        private static async Task<T> GetJsonResponse<T>(HttpRequestMessage request)
        {
            using (var client = new HttpClient())
            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return default;
                }

                string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(result);
            }
        }

        public static async Task<T> DoRequest<T>(object payload, string endpoint, string token)
        {
            HttpRequestMessage CreateRequest()
            {
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                    { Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json") };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return request;
            }

            using (var request = CreateRequest())
            {
                return await GetJsonResponse<T>(request).ConfigureAwait(false);
            }
        }

        public static int StrToInt(string s)
        {
            return int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static long StrToLong(string s)
        {
            return long.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
    }
}