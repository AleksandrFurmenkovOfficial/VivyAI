namespace VivyAi.Interfaces
{
    internal interface IChatFactory
    {
        IChat CreateChat(string chatId);
    }
}