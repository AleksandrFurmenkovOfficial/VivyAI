using RxTelegram.Bot.Interface.BaseTypes;
using RxTelegram.Bot.Interface.BaseTypes.Enums;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal class Messanger : IMessanger
    {
        private const long maxUniqueVisitors = 10;
        private const ParseMode parseMode = ParseMode.HTML;
        private readonly RxTelegram.Bot.TelegramBot bot;
        private readonly IDisposable listener;
        private readonly Subject<IChatMessage> messageSubject;
        private readonly Dictionary<string, string> nameMap = new();
        private readonly string adminId;

        public IObservable<IChatMessage> Message => messageSubject;

        public Messanger(string token, string adminId)
        {
            this.adminId = adminId;

            bot = new RxTelegram.Bot.TelegramBot(token);
            messageSubject = new Subject<IChatMessage>();
            listener = bot.Updates.Message.Subscribe(HandleMessage);

            User me = bot.GetMe().Result;
            Console.WriteLine($"@{me.Username} has started!");
        }

        public async Task<string> SendMessage(IChatMessage message)
        {
            var newMessage = await bot.SendMessage(new SendMessage
            {
                ChatId = long.Parse(message.chatId),
                Text = message.content,
                ParseMode = parseMode
            }).ConfigureAwait(false);

            return newMessage.MessageId.ToString();
        }

        public string AuthorName(IChatMessage message)
        {
            return nameMap[message.chatId];
        }

        public bool IsAdmin(IChatMessage message)
        {
            return message.chatId == adminId.ToString();
        }

        private string AuthorNameInternal(Message message)
        {
            string input = $"{message.From.FirstName}_{message.From.Username}_{message.From.LastName}".Trim().TrimStart('_').TrimEnd('_');
            return Regex.Replace(input, @"[^a-zA-Z0-9_]", string.Empty); ;
        }

        private bool HasAccess(IChatMessage message)
        {
            var chatId = message.chatId;
            bool isAdmin = IsAdmin(message);
            if (isAdmin)
            {
                _ = App.visitors.TryAdd(chatId, new Visitor(isAdmin, AuthorName(message)));
                return isAdmin;
            }

            Visitor visitor = App.visitors.GetOrAdd(chatId, (string id) => { Visitor arg = new(App.visitors.Count < maxUniqueVisitors, AuthorName(message)); return arg; });
            return visitor.access;
        }

        private void HandleMessage(Message message)
        {
            if (message.From.Id != message.Chat.Id)
            {
                return;
            }

            if (message.Text == null || message.Text.Length < 1)
            {
                var issueMessage = new ChatMessage()
                {
                    chatId = message.Chat.Id.ToString(),
                    content = Strings.OnlyText
                };
                _ = SendMessage(issueMessage).ConfigureAwait(false);
                return;
            }

            var chatId = message.Chat.Id.ToString();
            nameMap[chatId] = AuthorNameInternal(message);

            var newMessage = new ChatMessage()
            {
                chatId = chatId,
                name = nameMap[chatId],
                role = "user",
                content = message.Text
            };

            if (HasAccess(newMessage))
            {
                messageSubject.OnNext(newMessage);
            }
            else
            {
                _ = SendMessage(new ChatMessage
                {
                    chatId = chatId,
                    content = Strings.NoAccess
                }).ConfigureAwait(false);
            }
        }

        public Task NotifyAdmin(string message)
        {
            return SendMessage(new ChatMessage
            {
                chatId = adminId,
                content = message
            });
        }

        public Task EditMessage(string chatId, string messageId, string newContent)
        {
            return bot.EditMessageText(new EditMessageText
            {
                ChatId = long.Parse(chatId),
                MessageId = int.Parse(messageId),
                Text = newContent,
                ParseMode = parseMode
            });
        }
    }
}