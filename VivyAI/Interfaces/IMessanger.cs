namespace VivyAI.Interfaces
{
    internal interface IMessenger
    {
        const int maxTextLen = 2048;
        const int maxCaptionLen = 1024;

        Task<string> SendMessage(string chatId, IChatMessage message, IList<ActionId> messageActionIds = null);
        Task<string> SendPhotoMessage(string chatId, Uri image, string caption = null, IList<ActionId> messageActionIds = null);
        Task EditTextMessage(string chatId, string messageId, string content, IList<ActionId> messageActionIds = null);
        Task EditMessageCaption(string chatId, string messageId, string caption = null, IList<ActionId> messageActionIds = null);
        Task<bool> DeleteMessage(string chatId, string messageId);
        void NotifyAdmin(string message);
        bool IsAdmin(string chatId);
    }
}