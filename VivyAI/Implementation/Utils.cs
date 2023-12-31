using System.Globalization;
using Newtonsoft.Json;

namespace VivyAI.Implementation
{
    internal static class Utils
    {
        public static async Task<Stream> GetStreamFromUrlAsync(Uri url)
        {
            using var httpClient = new HttpClient();
            var bytes = await httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            return new MemoryStream(bytes);
        }

        public static async Task<string> EncodeImageToBase64(Uri imageUrl)
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl).ConfigureAwait(false);
            return Convert.ToBase64String(imageBytes);
        }

        public static async Task<object> GetJsonResponse(HttpRequestMessage request)
        {
            using var client = new HttpClient();
            using var response = await client.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject(result);
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