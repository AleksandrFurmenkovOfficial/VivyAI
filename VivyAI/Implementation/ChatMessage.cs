using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal sealed class ChatMessage(
        string messageId,
        string content,
        string role = "",
        string name = "",
        Uri imageUrl = null)
        : IChatMessage
    {
        public ChatMessage() : this(messageId: IChatMessage.InternalMessageId, "")
        {
        }

        public ChatMessage(string content, string role = "", string name = "", Uri imageUrl = null) : this(
            IChatMessage.InternalMessageId, content, role, name, imageUrl)
        {
        }

        public string MessageId { get; set; } = messageId;
        public string Content { get; set; } = content;
        public string Role { get; set; } = role;
        public string Name { get; set; } = name;
        public Uri ImageUrl { get; set; } = imageUrl;
    }
}