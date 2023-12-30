using RxTelegram.Bot;
using VivyAI.Interfaces;

namespace VivyAI.Implementation
{
    internal sealed class TelegramBotSource : ITelegramBotSource
    {
        private readonly string telegramBotKey;
        private ITelegramBot bot;

        public TelegramBotSource(string telegramBotKey)
        {
            this.telegramBotKey = telegramBotKey;
        }

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