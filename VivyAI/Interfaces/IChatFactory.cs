namespace VivyAI.Interfaces
{
    internal interface IChatFactory
    {
        IChat CreateChat(string chatId);
    }
}