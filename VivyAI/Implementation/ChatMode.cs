using Newtonsoft.Json;
using File = System.IO.File;

namespace VivyAi.Implementation
{
    internal sealed partial class Chat
    {
        public void SetEnglishTeacherMode()
        {
            SetMode(GetPath("EnglishTeacherMode"));
        }

        public void SetCommonMode()
        {
            SetMode(GetPath("CommonMode"));
        }

        private void AddMessageExample(string messageRole, string message)
        {
            if (messages.Count == 0)
            {
                messages.Add([]);
            }

            AddAnswerMessage(new ChatMessage
            {
                Role = messageRole,
                Name = messageRole == Strings.RoleUser ? $"Telegram_{messageRole}_Name" : aiAgent.AiName,
                Content = message
            });
        }

        private void SetMode(string modeDescriptionFilename)
        {
            string jsonString = File.ReadAllText(modeDescriptionFilename);
            var modeDescription = JsonConvert.DeserializeObject<ModeDescription>(jsonString);

            aiAgent.EnableFunctions = modeDescription?.EnableFunctions ?? false;
            var name = modeDescription?.AiName?.Trim();
            aiAgent.AiName = string.IsNullOrEmpty(name) ? Strings.DefaultName : name;
            var settings = modeDescription?.AiSettings?.Trim();
            aiAgent.SystemMessage = string.IsNullOrEmpty(settings) ? Strings.DefaultDescription : settings;

            if (modeDescription?.Messages == null)
            {
                return;
            }

            foreach (var example in modeDescription.Messages.Where(example =>
                         example is { Role: not null, Content: not null }))
            {
                AddMessageExample(example.Role, example.Content);
            }
        }

        private static string GetPath(string mode)
        {
            string directory = AppContext.BaseDirectory;
            return $"{directory}/Modes/{mode}.json";
        }

        private sealed class ModeDescription
        {
            [JsonProperty("enableFunctions")] public bool EnableFunctions { get; set; }

            [JsonProperty("aiName")] public string AiName { get; set; }

            [JsonProperty("aiSettings")] public string AiSettings { get; set; }

            [JsonProperty("messages")] public List<Example> Messages { get; set; }
        }

        private sealed class Example
        {
            [JsonProperty("role")] public string Role { get; set; }

            [JsonProperty("content")] public string Content { get; set; }
        }
    }
}