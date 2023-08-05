using Newtonsoft.Json;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI.Functions
{
    internal class WriteToVivyDiaryFunction : IFunction
    {
        internal sealed class WriteToVivyDiaryModel
        {
            [JsonPropertyName("info")]
            public string Info { get; set; }
        }

        public string name => "WriteToVivyDiary";

        public object Description()
        {
            return new JsonFunction
            {
                Name = name,
                Description = "This function allows Vivy to write a new entry in her diary (persistent storage between sessions).",
                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("info", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description = "The diary entry to be written (facts, thoughts, reasoning, conjectures, impressions)."
                    })
                    .AddRequired("info")
            };
        }

        public async Task<string> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = $"{userId}.txt";
            DateTime now = DateTime.Now;
            string timestamp = $"[{now.ToShortDateString()}|{now.ToShortTimeString()}]";
            string info = JsonConvert.DeserializeObject<WriteToVivyDiaryModel>(parameters).Info;
            string line = $"{timestamp}|{info}";
            await File.AppendAllTextAsync(path, line + Environment.NewLine);
            return "Information saved.";
        }
    }
}