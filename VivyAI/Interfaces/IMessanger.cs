using System;
using System.Threading.Tasks;

namespace VivyAI.Interfaces
{
    internal interface IMessanger
    {
        Task<string> SendMessage(IChatMessage message);
        Task EditMessage(string chatId, string messageId, string newContent);
        Task NotifyAdmin(string message);
        IObservable<IChatMessage> Message { get; }
        string AuthorName(IChatMessage message);
        bool IsAdmin(IChatMessage message);
    }
}