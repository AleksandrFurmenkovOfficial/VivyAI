using RxTelegram.Bot.Interface.BaseTypes;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed partial class Messenger : IMessenger
    {
        private const long maxUniqueVisitors = 10;
        private const string telegramFileBot = "https://api.telegram.org/file/bot";

        private RxTelegram.Bot.TelegramBot bot;
        private IDisposable messageListener;
        private IDisposable callbackListener;
        private readonly Func<string, IChatMessage, Task> messageHandler;
        private readonly Func<KeyValuePair<string, ActionParameters>, Task> actionHandler;
        private readonly ConcurrentDictionary<string, AppVisitor> visitors;

        private readonly ConcurrentDictionary<string, string> callbacksMapping = new();
        private readonly ConcurrentDictionary<string, string> nameMap = new();

        private readonly static Regex wrongNameSymbolsRegExp = MyRegex();

        private readonly string adminId;
        private readonly string token;

        public Messenger(
            string token,
            string adminId,
            Func<string, IChatMessage, Task> handleAppMessage,
            Func<KeyValuePair<string, ActionParameters>, Task> handleAppAction,
            ConcurrentDictionary<string, AppVisitor> visitors)
        {
            this.adminId = adminId;
            this.token = token;
            this.messageHandler = handleAppMessage;
            this.actionHandler = handleAppAction;
            this.visitors = visitors;

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

        private static string CompoundUserName(User user)
        {
            static string RemoveEmojis(string input)
            {
                static bool IsEmoji(char ch)
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
                    return unicodeCategory == UnicodeCategory.OtherSymbol;
                }

                var cleanText = new StringBuilder();
                foreach (var ch in input)
                {
                    if (!char.IsSurrogate(ch) && !IsEmoji(ch))
                    {
                        cleanText.Append(ch);
                    }
                }
                return cleanText.ToString();
            }

            string input = RemoveEmojis($"{user.FirstName}_{user.Username}_{user.LastName}");
            var result = wrongNameSymbolsRegExp.Replace(input, string.Empty).Replace(' ', '_').TrimStart('_').TrimEnd('_');
            if (result.Replace("_", string.Empty, StringComparison.InvariantCultureIgnoreCase).Length == 0)
            {
                result = $"User{user.Id}";
            }

            return result;
        }

        private bool HasAccess(Message message)
        {
            var chatId = message.Chat.Id.ToString(CultureInfo.InvariantCulture);
            var visitor = visitors.GetOrAdd(chatId, (id) =>
            {
                bool accessByDefault = visitors.Count < maxUniqueVisitors;
                if (accessByDefault || IsAdmin(chatId))
                {
                    var name = CompoundUserName(message.From);
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
            return $"{new Uri(new Uri($"{telegramFileBot}{token}/"), file.FilePath)}";
        }

        private async void HandleTelegramActionQuery(CallbackQuery callbackQuery)
        {
            if (callbacksMapping.Remove(callbackQuery.Data, out string callbackID))
            {
                await actionHandler(new KeyValuePair<string, ActionParameters>(
                    callbackID,
                    new ActionParameters(callbackQuery.From.Id.ToString(CultureInfo.InvariantCulture),
                    callbackQuery.Message.MessageId.ToString(CultureInfo.InvariantCulture)))).ConfigureAwait(false);
            }
        }

        private static bool IsOneToOneChat(Message rawMessage)
        {
            return rawMessage.From.Id == rawMessage.Chat.Id;
        }

        private async void HandleTelegramMessage(Message rawMessage)
        {
            if (!IsOneToOneChat(rawMessage))
            {
                return;
            }

            var chatId = rawMessage.Chat.Id.ToString(CultureInfo.InvariantCulture);
            if (!HasAccess(rawMessage))
            {
                _ = SendMessage(chatId, new ChatMessage(Strings.NoAccess));
                return;
            }

            var message = await ConvertToChatMessage(chatId, rawMessage).ConfigureAwait(false);
            if (message == null)
                return;

            await messageHandler(chatId, message).ConfigureAwait(false);
        }

        private async Task<ChatMessage> ConvertToChatMessage(string chatId, Message rawMessage)
        {
            bool isPhoto = rawMessage.Photo != null || rawMessage.ReplyToMessage?.Photo != null;
            bool isText =
                rawMessage.Text?.Length > 0 ||
                rawMessage.Caption?.Length > 0 ||
                rawMessage?.ReplyToMessage?.Text?.Length > 0 ||
                rawMessage?.ReplyToMessage?.Caption?.Length > 0;

            if (!isText && !isPhoto)
            {
                _ = SendMessage(chatId, new ChatMessage(Strings.OnlyTextOrPhoto));
                return null;
            }

            var resultMessage = new ChatMessage()
            {
                MessageId = rawMessage.MessageId.ToString(CultureInfo.InvariantCulture),
                Name = nameMap[chatId],
                Role = Strings.RoleUser
            };

            string content = "Messenger: telegram; Syntax: Markdown v2;\n";
            if (rawMessage.ForwardFrom != null)
            {
                content += $"User \"{CompoundUserName(rawMessage.From)}\" forwarded message from \"{CompoundUserName(rawMessage.ForwardFrom)}\"\n";
            }
            else if (rawMessage.ForwardFromChat != null)
            {
                content += $"User \"{CompoundUserName(rawMessage.From)}\" forwarded message from \"{rawMessage.ForwardFromChat.Title}\"(@{rawMessage.ForwardFromChat.Username})\n";
            }

            var replyTo = rawMessage.ReplyToMessage?.Text ?? rawMessage.ReplyToMessage?.Caption;
            if (!string.IsNullOrEmpty(replyTo) && replyTo.Length > 0)
            {
                content += $"Content of forwarded message: \"{replyTo}\"\n";
            }
            if (rawMessage.ReplyToMessage?.Photo != null)
            {
                content += $"{Strings.AttachedImage}: \"{await PhotoToLink(rawMessage.ReplyToMessage.Photo).ConfigureAwait(false)}\"";
            }

            var userText = rawMessage.Text ?? rawMessage.Caption;
            if (!string.IsNullOrEmpty(userText) && userText.Length > 0)
            {
                content += $"Content: \"{userText}\"\n";
            }
            if (rawMessage.Photo != null)
            {
                content += $"{Strings.AttachedImage}: \"{await PhotoToLink(rawMessage.Photo).ConfigureAwait(false)}\"";
            }

            resultMessage.Content = content;
            return resultMessage;
        }

        public bool IsAdmin(string chatId)
        {
            return chatId == adminId;
        }

        public void NotifyAdmin(string message)
        {
            _ = SendMessage(adminId, new ChatMessage(message));
        }

        [GeneratedRegex("[^a-zA-Z0-9_\\s-]")]
        private static partial Regex MyRegex();
    }
}