using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed class ChatMessage : IChatMessage
    {
        public ChatMessage() : this(messageId: IChatMessage.internalMessage, "")
        {
        }

        public ChatMessage(string content, string role = "", string name = "", Uri imageUrl = null) : this(IChatMessage.internalMessage, content, role, name, imageUrl)
        {
        }

        public ChatMessage(string messageId, string content, string role = "", string name = "", Uri imageUrl = null)
        {
            MessageId = messageId;
            Content = content;
            Role = role;
            Name = name;
            ImageUrl = imageUrl;
        }

        public string MessageId { get; set; }
        public string Content { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public Uri ImageUrl { get; set; }
    }
}