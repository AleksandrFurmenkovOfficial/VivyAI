using Newtonsoft.Json;
using System.Reflection;
using VivyAI.Interfaces;
using File = System.IO.File;

namespace VivyAI
{
    internal sealed partial class Chat : IChat, IDisposable
    {
        private sealed class ModeDescription
        {
            [JsonProperty("enableFunctions")]
            public bool EnableFunctions { get; set; }

            [JsonProperty("aiName")]
            public string AiName { get; set; }

            [JsonProperty("aiSettings")]
            public string AiSettings { get; set; }

            [JsonProperty("messages")]
            public List<Example> Messages { get; set; }
        }

        private sealed class Example
        {
            [JsonProperty("role")]
            public string Role { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }

        private void AddMessageExample(string messageRole, string message)
        {
            if (messages.Count == 0)
                messages.Add(new List<IChatMessage>());

            AddAnswerMessage(new ChatMessage
            {
                Role = messageRole,
                Name = messageRole == Strings.RoleUser ? $"Telegram_{messageRole}_Name" : openAI.AIName,
                Content = message
            });
        }

        private void SetMode(string modeDescriptionFilename)
        {
            string jsonString = File.ReadAllText(modeDescriptionFilename);
            var modeDescription = JsonConvert.DeserializeObject<ModeDescription>(jsonString);

            openAI.EnableFunctions = modeDescription?.EnableFunctions ?? false;
            var name = modeDescription?.AiName?.Trim();
            openAI.AIName = string.IsNullOrEmpty(name) ? Strings.DefaultName : name;
            var settings = modeDescription?.AiSettings?.Trim();
            openAI.SystemMessage = string.IsNullOrEmpty(settings) ? Strings.DefaultDescription : settings;

            if (modeDescription.Messages != null)
            {
                foreach (var example in modeDescription.Messages)
                {
                    if (example != null && example.Role != null && example.Content != null)
                    {
                        AddMessageExample(example.Role, example.Content);
                    }
                }
            }
        }

        private static string GetPath(string mode)
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return $"{directory}/Modes/{mode}.json";
        }

        public void SetEnglishTeacherMode()
        {
            SetMode(GetPath("EnglishTeacherMode"));
        }

        public void SetCommonMode()
        {
            SetMode(GetPath("CommonMode"));
        }
    }
}