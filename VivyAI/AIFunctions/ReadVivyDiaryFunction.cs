using Newtonsoft.Json.Linq;
using OpenAI_API.ChatFunctions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class ReadVivyDiaryFunction : IFunction
    {
        public string name => "ReadVivyDiary";

        public object Description()
        {
            var parameters = new JObject()
            {
                ["type"] = "object",
                ["required"] = new JArray(),
                ["properties"] = new JObject()
            };

            string functionDescription = "This function allows Vivy to read the last nine entries from her diary.";
            return new Function(name, functionDescription, parameters);
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = $"{userId}.txt";
            if (!File.Exists(path))
            {
                return "No information available.";
            }

            string data = await File.ReadAllTextAsync(path);
            string firstNineRecordsAsString = string.Join(Environment.NewLine, data.Split(Environment.NewLine).ToList().Take(9));
            return firstNineRecordsAsString;
        }
    }
}