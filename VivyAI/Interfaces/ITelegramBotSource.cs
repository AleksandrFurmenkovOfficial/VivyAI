using RxTelegram.Bot;

namespace VivyAi.Interfaces
{
    internal interface ITelegramBotSource
    {
        void RecreateTelegramBot();
        ITelegramBot GetTelegramBot();
    }
}