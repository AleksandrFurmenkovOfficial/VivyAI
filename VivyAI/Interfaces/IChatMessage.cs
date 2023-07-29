namespace VivyAI.Interfaces
{
    public interface IChatMessage
    {
        string chatId { get; set; }
        string name { get; set; }
        string content { get; set; }
        string role { get; set; }
    }
}