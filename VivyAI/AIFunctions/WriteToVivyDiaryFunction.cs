using Newtonsoft.Json.Linq;
using OpenAI_API.ChatFunctions;
using System;
using System.IO;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class WriteToVivyDiaryFunction : IFunction
    {
        public string name => "WriteToVivyDiary";

        public object Description()
        {
            JObject parameters = new()
            {
                ["type"] = "object",
                ["required"] = new JArray("info"),
                ["properties"] = new JObject
                {
                    ["info"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The diary entry to be written (facts, thoughts, reasoning, conjectures, impressions)."
                    }
                }
            };

            string functionDescription = "This function allows Vivy to write a new entry in her diary (persistent storage between sessions).";
            return new Function(name, functionDescription, parameters);
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = $"{userId}.txt";
            DateTime now = DateTime.Now;
            string timestamp = $"[{now.ToShortDateString()}|{now.ToShortTimeString()}]";
            string line = $"{timestamp}|{parameters.info}";
            await File.AppendAllTextAsync(path, line + Environment.NewLine);
            return "Information saved.";
        }
    }
}