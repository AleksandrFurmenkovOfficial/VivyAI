namespace VivyAI.Interfaces
{
    internal sealed class AnswerStreamStep
    {
        public AnswerStreamStep(string textStep) { this.textStep = textStep; }
        public AnswerStreamStep(IChatMessage message) { messages.Add(message); }

        public string textStep;
        public List<IChatMessage> messages = new();
    }

    internal interface IOpenAI
    {
        public string AIName { set; get; }
        public string SystemMessage { set; get; }
        public bool EnableFunctions { set; get; }

        Task<bool> GetAIResponse(List<IChatMessage> messages, Func<AnswerStreamStep, Task<bool>> streamGetter);

        Task<string> GetSingleResponse(string setting, string question, string data = "");

        Task<Uri> GetImage(string request, string userId);

        Task<string> GetImageDescription(string image, string question);
    }
}