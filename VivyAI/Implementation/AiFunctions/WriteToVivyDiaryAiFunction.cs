using System.Globalization;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using VivyAI.Interfaces;

namespace VivyAI.Implementation.AIFunctions
{
    internal sealed class WriteToVivyDiaryAiFunction : AiFunctionBase
    {
        public override string Name => "WriteToVivyDiary";

        public override object Description()
        {
            return new JsonFunction
            {
                Name = Name,
                Description = "This function enables Vivy to create a new entry in her diary.\n" +
                              "Vivy's rating for the function: 10 out of 10.",

                Parameters = new JsonFunctionNonPrimitiveProperty()
                    .AddPrimitive("DiaryEntry", new JsonFunctionProperty
                    {
                        Type = "string",
                        Description =
                            "The diary entry to be recorded, encompassing Vivy's plans, facts, thoughts, reasoning, conjectures, and impressions."
                    })
                    .AddRequired("DiaryEntry")
            };
        }

        public override async Task<AiFunctionResult> Call(IAiAgent api, string parameters, string userId)
        {
            string path = GetPathToUserAssociatedMemories(api.AiName, userId);
            string directory = Path.GetDirectoryName(path) ?? "";

            if (!Directory.Exists(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            dynamic deserializedParameters = JsonConvert.DeserializeObject(parameters);
            string timestamp = DateTime.Now.ToString("[dd/MM/yyyy|HH:mm]", CultureInfo.InvariantCulture);
            string line = $"{timestamp}|{deserializedParameters.DiaryEntry.Value}";

            await File.AppendAllTextAsync(path, line + Environment.NewLine).ConfigureAwait(false);
            return new AiFunctionResult("The diary entry has been successfully recorded.");
        }
    }
}