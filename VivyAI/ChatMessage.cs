using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed class ChatMessage : IChatMessage
    {
        public ChatMessage()
        {
        }

        public ChatMessage(string content, string role = "", string name = "", Uri imageUrl = null)
        {
            Content = content;
            Role = role;
            Name = name;
            ImageUrl = imageUrl;
        }

        public ChatMessage(string id, string content, string role = "", string name = "", Uri imageUrl = null)
        {
            Id = id;
            Content = content;
            Role = role;
            Name = name;
            ImageUrl = imageUrl;
        }

        public string Id { get; set; }
        public string Content { get; set; }
        public string Role { get; set; }
        public string Name { get; set; }
        public Uri ImageUrl { get; set; }
    }
}