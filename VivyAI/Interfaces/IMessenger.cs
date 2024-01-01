namespace VivyAi.Interfaces
{
    internal interface IMessenger
    {
        const int MaxTextLen = 2048;
        const int MaxCaptionLen = 1024;

        Task<string> SendMessage(string chatId, IChatMessage message, IEnumerable<ActionId> messageActionIds = null);

        Task<string> SendPhotoMessage(string chatId, Uri image, string caption = null,
            IEnumerable<ActionId> messageActionIds = null);

        Task EditTextMessage(string chatId, string messageId, string content,
            IEnumerable<ActionId> messageActionIds = null);

        Task EditMessageCaption(string chatId, string messageId, string caption = null,
            IEnumerable<ActionId> messageActionIds = null);

        Task<bool> DeleteMessage(string chatId, string messageId);
    }
}