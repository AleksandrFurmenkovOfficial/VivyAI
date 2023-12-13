using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class WriteToVivyDiaryFunction : IFunction
    {
        internal sealed class WriteToVivyDiaryModel
        {
            [JsonPropertyName("DiaryRecord")]
            public string DiaryRecord { get; set; }
        }

        public string Name => "WriteToVivyDiary";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "The function allows Vivy to write a new entry in her diary(How much Vivy like the function: 10/10).",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("DiaryRecord", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "The diary note to be written (Vivy's plans, facts, thoughts, reasoning, conjectures, impressions)."
                })
                .AddRequired("DiaryRecord")
        };

        public async Task<FuncResult> Call(IOpenAI api, dynamic parameters, string userId)
        {
            string path = Utils.GetPathToUserAssociatedMemories(api.AIName, userId);
            string directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            var model = JsonConvert.DeserializeObject<WriteToVivyDiaryModel>(parameters);
            string timestamp = DateTime.Now.ToString("[dd/MM/yyyy|HH:mm]", CultureInfo.InvariantCulture);
            string line = $"{timestamp}|{model.DiaryRecord}";

            await File.AppendAllTextAsync(path, line + Environment.NewLine);
            return new FuncResult("Information saved.");
        }
    }
}