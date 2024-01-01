using RxTelegram.Bot;
using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal sealed class TelegramBotSource(string telegramBotKey) : ITelegramBotSource
    {
        private ITelegramBot bot;

        public ITelegramBot GetTelegramBot()
        {
            return bot;
        }

        public void RecreateTelegramBot()
        {
            bot = new TelegramBot(telegramBotKey);
        }
    }
}