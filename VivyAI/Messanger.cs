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
        private readonly Action<string, IChatMessage> handleAppMessage;
        private readonly Action<KeyValuePair<string, ActionParameters>> handleAppAction;

        private readonly ConcurrentDictionary<string, string> callbacksMapping = new();
        private readonly ConcurrentDictionary<string, string> nameMap = new();
        private readonly static Regex WrongNameSymbolsRegExp = new("^a-zA-Z0-9_");

        private readonly string adminId;
        private readonly string token;

        public Messanger(
            string token,
            string adminId,
            Action<string, IChatMessage> handleAppMessage,
            Action<KeyValuePair<string, ActionParameters>> handleAppAction)
        {
            this.adminId = adminId;
            this.token = token;
            this.handleAppMessage = handleAppMessage;
            this.handleAppAction = handleAppAction;

            _ = ReCreate();
        }

        private async Task ReCreate()
        {
            this.bot = new RxTelegram.Bot.TelegramBot(token);

            this.messageListener?.Dispose();
            this.messageListener = bot.Updates.Message.Subscribe(HandleTelegramMessage, OnError);

            this.callbackListener?.Dispose();
            this.callbackListener = bot.Updates.CallbackQuery.Subscribe(HandleTelegramActionQuery, OnError);

            Console.WriteLine($"@{(await bot.GetMe().ConfigureAwait(false)).Username} has started!");
        }

        private void OnError(Exception e)
        {
            _ = ReCreate();
            App.LogException(e);
        }

        public async Task<string> SendMessage(string chatId, IChatMessage message, IList<ActionId> messageActionIds = null)
        {
            var newMessage = await bot.SendMessage(new SendMessage
            {
                ChatId = Utils.StrToLong(chatId),
                Text = message.Content,
                ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds),
                ParseMode = parseMode
            }).ConfigureAwait(false);

            return newMessage.MessageId.ToString(CultureInfo.InvariantCulture);
        }

        private InlineKeyboardMarkup GetInlineKeyboardMarkup(IList<ActionId> messageActionIds)
        {
            InlineKeyboardMarkup inlineKeyboardMarkup = null;
            if (messageActionIds != null && messageActionIds.Any())
            {
                callbacksMapping.Clear();
                var buttons = new List<InlineKeyboardButton>();
                foreach (var callbackId in messageActionIds)
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
            var file = await bot.GetFile(photoSize.FileId).ConfigureAwait(false);
            var baseUrl = new Uri($"{telegramFileBot}{token}/");
            return new Uri(baseUrl, file.FilePath).AbsoluteUri;
        }

        private void HandleTelegramActionQuery(CallbackQuery callbackQuery)
        {
            if (callbacksMapping.Remove(callbackQuery.Data, out string callbackID))
            {
                handleAppAction(new KeyValuePair<string, ActionParameters>(
                    callbackID,
                    new ActionParameters(callbackQuery.From.Id.ToString(CultureInfo.InvariantCulture),
                    callbackQuery.Message.MessageId.ToString(CultureInfo.InvariantCulture))));
            }
        }

        private async void HandleTelegramMessage(Message message)
        {
            if (message.From.Id != message.Chat.Id)
            {
                return;
            }

            bool isPhoto = message.Photo != null;
            bool isText =
                message.Text?.Length >= 1 ||
                message.Caption?.Length >= 1 ||
                message.ReplyToMessage?.Text?.Length >= 1 ||
                message.ReplyToMessage?.Caption?.Length >= 1;

            var chatId = message.Chat.Id.ToString(CultureInfo.InvariantCulture);
            if (!isText && !isPhoto)
            {
                _ = SendMessage(chatId, new ChatMessage(Strings.OnlyTextOrPhoto));
                return;
            }

            if (!HasAccess(message))
            {
                _ = SendMessage(chatId, new ChatMessage(Strings.NoAccess));
                return;
            }

            var attachedPhotoString = "";
            if (isPhoto)
            {
                attachedPhotoString = $"{Strings.AttachedImage}: \"{await PhotoToLink(message.Photo).ConfigureAwait(false)}\"";
                if (isText)
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
                else
                {
                    message.Text = attachedPhotoString;
                }
            }

            var textOnReply = message.ReplyToMessage?.Text != null ? $"{Strings.Quote}: '{message.ReplyToMessage?.Text}'\n{Strings.UserComment}: '{message.Text ?? message.Caption}'" : null;
            var textOnReplyCaption = message.ReplyToMessage?.Caption != null ? $"{Strings.Quote}: '{message.ReplyToMessage?.Caption}'\n{Strings.UserComment}: '{message.Text ?? message.Caption}'" : null;

            var newMessage = new ChatMessage()
            {
                MessageId = message.MessageId.ToString(CultureInfo.InvariantCulture),
                Name = nameMap[chatId],
                Role = Strings.RoleUser,
                Content = textOnReply ??
                          textOnReplyCaption ??
                          message.Text ??
                          message.Caption ??
                          string.Empty
            };

            handleAppMessage(chatId, newMessage);
        }

        public void NotifyAdmin(string message)
        {
            _ = SendMessage(adminId, new ChatMessage(message));
        }

        public async Task EditTextMessage(string chatId, string messageId, string newContent, IList<ActionId> messageActionIds = null)
        {
            _ = await bot.EditMessageText(new EditMessageText
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.StrToInt(messageId),
                Text = newContent,
                ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds),
                ParseMode = parseMode
            }).ConfigureAwait(false);
        }

        public async Task<bool> DeleteMessage(string chatId, string messageId)
        {
            return await bot.DeleteMessage(new DeleteMessage
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.StrToInt(messageId)
            }).ConfigureAwait(false);
        }

        public async Task<string> SendPhotoMessage(string chatId, Uri imageUrl, string caption, IList<ActionId> messageActionIds = null)
        {
            using var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl).ConfigureAwait(false);
            return (await bot.SendPhoto(new SendPhoto
            {
                ChatId = Utils.StrToLong(chatId),
                Photo = new InputFile(imageStream),
                Caption = caption,
                ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds)
            }).ConfigureAwait(false)).MessageId.ToString(CultureInfo.InvariantCulture);
        }

        public async Task EditMessageCaption(string chatId, string messageId, string caption, IList<ActionId> messageActionIds = null)
        {
            await bot.EditMessageCaption(new EditMessageCaption
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.StrToInt(messageId),
                Caption = caption,
                ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds)
            }).ConfigureAwait(false);
        }
    }
}