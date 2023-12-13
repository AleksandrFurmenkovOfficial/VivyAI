namespace VivyAI.Interfaces
{
    internal interface IMessanger
    {
        Task<string> SendMessage(string chatId, IChatMessage message, IList<CallbackId> messageCallbackIds = null);
        Task EditTextMessage(string chatId, string messageId, string newContent, IList<CallbackId> messageCallbackIds = null);
        Task<bool> DeleteMessage(string chatId, string messageId);
        Task<string> SendPhotoMessage(string chatId, Uri image, string caption = null);
        void NotifyAdmin(string message);
        bool IsAdmin(string chatId);
    }
}