namespace VivyAI.Interfaces
{
    public interface IChatMessage
    {
        const string internalMessage = "0";

        string MessageId { get; set; }
        string Name { get; set; }
        string Content { get; set; }
        string Role { get; set; }
        public Uri ImageUrl { get; set; }
    }
}