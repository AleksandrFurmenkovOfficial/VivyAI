using Newtonsoft.Json;

namespace VivyAI
{
    internal static class Utils
    {
        public static async Task<Stream> GetStreamFromUrlAsync(Uri url)
        {
            using var httpClient = new HttpClient();
            var bytes = await httpClient.GetByteArrayAsync(url);
            return new MemoryStream(bytes);
        }

        public static async Task<string> EncodeImageToBase64(Uri imageUrl)
        {
            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);
            return Convert.ToBase64String(imageBytes);
        }

        public static async Task<dynamic> GetJsonResponse(HttpRequestMessage request)
        {
            using var client = new HttpClient();
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                return null;
            }

            string result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject(result);
        }

        public static int StrToInt(string s)
        {
            return int.Parse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static long StrToLong(string s)
        {
            return long.Parse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}