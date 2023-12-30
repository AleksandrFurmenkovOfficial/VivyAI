using RxTelegram.Bot;

namespace VivyAI.Interfaces
{
    internal interface ITelegramBotSource
    {
        void RecreateTelegramBot();
        ITelegramBot GetTelegramBot();
    }
}