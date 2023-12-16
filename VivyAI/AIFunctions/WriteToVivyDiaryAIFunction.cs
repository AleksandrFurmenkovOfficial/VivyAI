using Newtonsoft.Json;
using System.Globalization;
using System.Text.Json.Serialization;
using VivyAI.Interfaces;

namespace VivyAI.AIFunctions
{
    internal sealed class WriteToVivyDiaryAIFunction : IAIFunction
    {
        public string Name => "WriteToVivyDiary";

        public object Description() => new JsonFunction
        {
            Name = Name,
            Description = "This function enables Vivy to create a new entry in her diary.\nVivy's rating for the function: 10 out of 10.",
            Parameters = new JsonFunctionNonPrimitiveProperty()
                .AddPrimitive("DiaryEntry", new JsonFunctionProperty
                {
                    Type = "string",
                    Description = "The diary entry to be recorded, encompassing Vivy's plans, facts, thoughts, reasoning, conjectures, and impressions."
                })
                .AddRequired("DiaryEntry")
        };

        public async Task<AIFunctionResult> Call(IAIAgent api, dynamic parameters, string userId)
        {
            string path = Utils.GetPathToUserAssociatedMemories(api.AIName, userId);
            string directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            var deserializedParameters = JsonConvert.DeserializeObject(parameters);
            string timestamp = DateTime.Now.ToString("[dd/MM/yyyy|HH:mm]", CultureInfo.InvariantCulture);
            string line = $"{timestamp}|{deserializedParameters.DiaryEntry.Value}";

            await File.AppendAllTextAsync(path, line + Environment.NewLine).ConfigureAwait(false);
            return new AIFunctionResult("The diary entry has been successfully recorded.");
        }
    }
}