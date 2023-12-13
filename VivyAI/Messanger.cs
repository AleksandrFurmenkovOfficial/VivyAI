using RxTelegram.Bot.Interface.BaseTypes;
using RxTelegram.Bot.Interface.BaseTypes.Enums;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Attachments;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed class Messanger : IMessanger
    {
        private const long maxUniqueVisitors = 10;
        private const ParseMode parseMode = ParseMode.HTML;
        private const string telegramFileBot = "https://api.telegram.org/file/bot";

        private RxTelegram.Bot.TelegramBot bot;
        private IDisposable messageListener;
        private IDisposable callbackListener;
        private readonly Action<KeyValuePair<string, IChatMessage>> handleAppMessage;
        private readonly Action<KeyValuePair<string, CallbackCallId>> handleAppCallback;

        private readonly ConcurrentDictionary<string, string> callbacksMapping = new();
        private readonly ConcurrentDictionary<string, string> nameMap = new();
        private readonly static Regex WrongNameSymbolsRegExp = new("^a-zA-Z0-9_");

        private readonly string adminId;
        private readonly string token;

        public Messanger(
            string token,
            string adminId,
            Action<KeyValuePair<string, IChatMessage>> handleAppMessage,
            Action<KeyValuePair<string, CallbackCallId>> handleAppCallback)
        {
            this.adminId = adminId;
            this.token = token;
            this.handleAppMessage = handleAppMessage;
            this.handleAppCallback = handleAppCallback;

            _ = ReCreate();
        }

        private async Task ReCreate()
        {
            this.bot = new RxTelegram.Bot.TelegramBot(token);

            this.messageListener?.Dispose();
            this.messageListener = bot.Updates.Message.Subscribe(HandleTelegramMessage, OnError);

            this.callbackListener?.Dispose();
            this.callbackListener = bot.Updates.CallbackQuery.Subscribe(HandleTelegramCallbackQuery, OnError);

            Console.WriteLine($"@{(await bot.GetMe()).Username} has started!");
        }

        private void OnError(Exception obj)
        {
            Console.WriteLine($"Messanger.OnError: {obj.Message}\nStack:\n{obj.StackTrace}");
            _ = ReCreate();
        }

        public async Task<string> SendMessage(string chatId, IChatMessage message, IList<CallbackId> messageCallbackIds = null)
        {
            var newMessage = await bot.SendMessage(new SendMessage
            {
                ChatId = Utils.StrToLong(chatId),
                Text = message.Content,
                ReplyMarkup = GetInlineKeyboardMarkup(messageCallbackIds),
                ParseMode = parseMode
            });

            return newMessage.MessageId.ToString(CultureInfo.InvariantCulture);
        }

        private InlineKeyboardMarkup GetInlineKeyboardMarkup(IList<CallbackId> messageCallbackIds)
        {
            InlineKeyboardMarkup inlineKeyboardMarkup = null;
            if (messageCallbackIds != null && messageCallbackIds.Any())
            {
                callbacksMapping.Clear();
                var buttons = new List<InlineKeyboardButton>();
                foreach (var callbackId in messageCallbackIds)
                {
                    var token = Guid.NewGuid().ToString();
                    _ = callbacksMapping.TryAdd(token, callbackId.name);
                    buttons.Add(new InlineKeyboardButton
                    {
                        Text = callbackId.name,
                        CallbackData = token
                    });
                }

                inlineKeyboardMarkup = new InlineKeyboardMarkup
                {
                    InlineKeyboard = new[] { buttons.ToArray() }
                };
            }

            return inlineKeyboardMarkup;
        }

        public bool IsAdmin(string chatId)
        {
            return chatId == adminId;
        }

        private static string AuthorNameInternal(Message message)
        {
            string input = $"{message.From.FirstName}_{message.From.Username}_{message.From.LastName}".Trim().TrimStart('_').TrimEnd('_');
            return WrongNameSymbolsRegExp.Replace(input, string.Empty); ;
        }

        private bool HasAccess(Message message)
        {
            var chatId = message.Chat.Id.ToString(CultureInfo.InvariantCulture);
            var visitor = App.visitors.GetOrAdd(chatId, (id) =>
            {
                bool accessByDefault = App.visitors.Count < maxUniqueVisitors;
                if (accessByDefault || IsAdmin(chatId))
                {
                    var name = AuthorNameInternal(message);
                    _ = nameMap.AddOrUpdate(chatId, name, (_, _) => name);
                }

                var arg = new AppVisitor(accessByDefault, nameMap[chatId]);
                return arg;
            });

            return visitor.access;
        }

        private async Task<string> PhotoToLink(IEnumerable<PhotoSize> photos)
        {
            var photoSize = photos.Last();
            var file = await bot.GetFile(photoSize.FileId);
            var baseUrl = new Uri($"{telegramFileBot}{token}/");
            return new Uri(baseUrl, file.FilePath).AbsoluteUri;
        }

        private void HandleTelegramCallbackQuery(CallbackQuery callbackQuery)
        {
            if (callbacksMapping.Remove(callbackQuery.Data, out string callbackID))
            {
                _ = Task.Run(() =>
                {
                    handleAppCallback(new KeyValuePair<string, CallbackCallId>(callbackID, new CallbackCallId(callbackQuery.From.Id.ToString(CultureInfo.InvariantCulture), callbackQuery.Message.MessageId.ToString(CultureInfo.InvariantCulture))));
                });
            }
        }

        private async void HandleTelegramMessage(Message message)
        {
            if (message.From.Id != message.Chat.Id)
            {
                return;
            }


            var attachedPhotoString = "";
            bool isPhoto = message.Photo != null;
            if (isPhoto)
            {
                attachedPhotoString = $"{Strings.AttachedImage}: \"{await PhotoToLink(message.Photo)}\"";
            }

            bool isText = message.Text?.Length >= 1 || message.Caption?.Length >= 1 || message.ReplyToMessage?.Text?.Length >= 1 || message.ReplyToMessage?.Caption?.Length >= 1;
            var chatId = message.Chat.Id.ToString(CultureInfo.InvariantCulture);
            if (!isText && !isPhoto)
            {
                _ = SendMessage(chatId, new ChatMessage(Strings.OnlyTextOrPhoto));
                return;
            }

            if (isText && isPhoto)
            {
                if (!string.IsNullOrEmpty(message.Text))
                {
                    message.Text += $"\n{attachedPhotoString}";
                }
                else
                {
                    message.Caption += $"\n{attachedPhotoString}";
                }
            }
            else if (isPhoto)
            {
                message.Text = attachedPhotoString;
            }

            if (!HasAccess(message))
            {
                _ = SendMessage(chatId, new ChatMessage(Strings.NoAccess));
            }

            var textOnReply = message.ReplyToMessage?.Text != null ? $"{Strings.Quote}: '{message.ReplyToMessage?.Text}'\n{Strings.UserComment}: '{message.Text ?? message.Caption}'" : null;
            var textOnReplyCaption = message.ReplyToMessage?.Caption != null ? $"{Strings.Quote}: '{message.ReplyToMessage?.Caption}'\n{Strings.UserComment}: '{message.Text ?? message.Caption}'" : null;

            var newMessage = new ChatMessage()
            {
                Id = message.MessageId.ToString(CultureInfo.InvariantCulture),
                Name = nameMap[chatId],
                Role = Strings.RoleUser,
                Content = textOnReply ?? textOnReplyCaption ?? message.Text ?? message.Caption ?? string.Empty
            };

            _ = Task.Run(() =>
            {
                handleAppMessage(new KeyValuePair<string, IChatMessage>(chatId, newMessage));
            });
        }

        public void NotifyAdmin(string message)
        {
            _ = SendMessage(adminId, new ChatMessage(message));
        }

        public async Task EditTextMessage(string chatId, string messageId, string newContent, IList<CallbackId> messageCallbackIds = null)
        {
            _ = await bot.EditMessageText(new EditMessageText
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.StrToInt(messageId),
                Text = newContent,
                ReplyMarkup = GetInlineKeyboardMarkup(messageCallbackIds),
                ParseMode = parseMode
            });
        }

        public async Task<bool> DeleteMessage(string chatId, string messageId)
        {
            return await bot.DeleteMessage(new DeleteMessage
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.StrToInt(messageId)
            });
        }

        public async Task<string> SendPhotoMessage(string chatId, Uri imageUrl, string caption)
        {
            using var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl);
            return (await bot.SendPhoto(new SendPhoto
            {
                ChatId = Utils.StrToLong(chatId),
                Photo = new InputFile(imageStream),
                Caption = caption
            })).MessageId.ToString(CultureInfo.InvariantCulture);
        }
    }
}