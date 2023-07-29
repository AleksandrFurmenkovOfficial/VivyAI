using VivyAI.Interfaces;

namespace VivyAI
{
    internal class ChatMessage : IChatMessage
    {
        public string chatId { get; set; }
        public string content { get; set; }
        public string role { get; set; }
        public string name { get; set; }
    }
}