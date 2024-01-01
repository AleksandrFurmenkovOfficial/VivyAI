using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using RxTelegram.Bot;
using RxTelegram.Bot.Interface.BaseTypes;
using VivyAi.Implementation.ChatCommands;
using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal sealed partial class ChatProcessor(
        string telegramBotKey,
        ConcurrentDictionary<string, IAppVisitor> visitors,
        ConcurrentDictionary<string, ConcurrentDictionary<string, ActionId>> callbacksMapping,
        IAdminChecker adminChecker,
        IChatFactory chatFactory,
        IChatMessageProcessor chatMessageProcessor,
        IChatMessageActionProcessor chatMessageActionProcessor,
        ITelegramBotSource botSource)
        : IChatProcessor
    {
        private const long MaxUniqueVisitors = 10;
        private const string TelegramFileBot = "https://api.telegram.org/file/bot";

        private static readonly Regex WrongNameSymbolsRegExp = WrongNameSymbolsRegexpCreator();

        private readonly ConcurrentDictionary<string, IChat> chatById = [];

        private readonly ConcurrentDictionary<string, string> nameMap = [];

        private IDisposable callbackListener;
        private IDisposable messageListener;

        private ITelegramBot Bot => botSource.GetTelegramBot();

        public async Task Run()
        {
            await ReCreate().ConfigureAwait(false);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private async Task ReCreate()
        {
            botSource.RecreateTelegramBot();

            messageListener?.Dispose();
            messageListener = Bot.Updates.Message.Subscribe(HandleTelegramMessage, OnError);

            callbackListener?.Dispose();
            callbackListener = Bot.Updates.CallbackQuery.Subscribe(HandleTelegramActionQuery, OnError);

            Console.WriteLine(Strings.HasStarted, (await Bot.GetMe().ConfigureAwait(false)).Username);
        }

        private void OnError(Exception e)
        {
            _ = ReCreate();
            ExceptionHandler.LogException(e);
        }

        private static string CompoundUserName(User user)
        {
            string input = RemoveEmojis($"{user.FirstName}_{user.Username}_{user.LastName}");
            var result = WrongNameSymbolsRegExp.Replace(input, string.Empty).Replace(' ', '_').TrimStart('_')
                .TrimEnd('_');
            if (result.Replace("_", string.Empty, StringComparison.InvariantCultureIgnoreCase).Length == 0)
            {
                result = $"User{user.Id}";
            }

            return result.Replace("__", "_", StringComparison.InvariantCultureIgnoreCase);

            static string RemoveEmojis(string input)
            {
                var cleanText = new StringBuilder();
                foreach (var ch in input.Where(ch => !char.IsSurrogate(ch) && !IsEmoji(ch)))
                {
                    cleanText.Append(ch);
                }

                return cleanText.ToString();

                static bool IsEmoji(char ch)
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
                    return unicodeCategory == UnicodeCategory.OtherSymbol;
                }
            }
        }

        private bool HasAccess(Message message)
        {
            var chatId = message.Chat.Id.ToString(CultureInfo.InvariantCulture);
            var visitor = visitors.GetOrAdd(chatId, id =>
            {
                bool accessByDefault = visitors.Count < MaxUniqueVisitors;
                if (accessByDefault || adminChecker.IsAdmin(chatId))
                {
                    var name = CompoundUserName(message.From);
                    _ = nameMap.AddOrUpdate(chatId, name, (_, _) => name);
                }

                var arg = new AppVisitor(accessByDefault, nameMap[chatId]);
                return arg;
            });

            return visitor.Access;
        }

        private async Task<string> PhotoToLink(IEnumerable<PhotoSize> photos)
        {
            var photoSize = photos.Last();
            var file = await Bot.GetFile(photoSize.FileId).ConfigureAwait(false);
            return $"{new Uri(new Uri($"{TelegramFileBot}{telegramBotKey}/"), file.FilePath)}";
        }

        private async void HandleTelegramActionQuery(CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery?.From?.Id.ToString(CultureInfo.InvariantCulture);
            if (chatId == null)
                return;

            var mapping = callbacksMapping.GetOrAdd(chatId, (_) => []);
            if (!mapping.Remove(callbackQuery.Data, out var callbackId))
            {
                return;
            }

            if (!chatById.TryGetValue(chatId, out var chat))
            {
                return;
            }

            await chatMessageActionProcessor.HandleMessageAction(
                    chat,
                    new ActionParameters(
                        callbackId,
                        callbackQuery.Message.MessageId.ToString(CultureInfo.InvariantCulture)))
                .ConfigureAwait(false);
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
                var tmpChat = GetOrCreateChat(chatId);
                await tmpChat.SendSystemMessage(Strings.NoAccess).ConfigureAwait(false);
                chatById.Remove(chatId, out _);
                (tmpChat as IDisposable)?.Dispose();
                return;
            }

            var message = await ConvertToChatMessage(chatId, rawMessage).ConfigureAwait(false);
            if (message == null)
            {
                return;
            }

            var chat = GetOrCreateChat(chatId);
            await chatMessageProcessor.HandleMessage(chat, message).ConfigureAwait(false);
        }

        private IChat GetOrCreateChat(string chatId)
        {
            return chatById.GetOrAdd(chatId, _ => chatFactory.CreateChat(chatId));
        }

        private async Task<ChatMessage> ConvertToChatMessage(string chatId, Message rawMessage)
        {
            bool isPhoto = rawMessage.Photo != null || rawMessage.ReplyToMessage?.Photo != null;
            bool isText =
                rawMessage.Text?.Length > 0 ||
                rawMessage.Caption?.Length > 0 ||
                rawMessage.ReplyToMessage?.Text?.Length > 0 ||
                rawMessage.ReplyToMessage?.Caption?.Length > 0;

            if (!isText && !isPhoto)
            {
                var chat = GetOrCreateChat(chatId);
                await chat.SendSystemMessage(Strings.OnlyTextOrPhoto).ConfigureAwait(false);
                return null;
            }

            var resultMessage = new ChatMessage
            {
                MessageId = rawMessage.MessageId.ToString(CultureInfo.InvariantCulture),
                Name = nameMap[chatId],
                Role = Strings.RoleUser
            };


            string content = "Messenger: telegram; Syntax: Markdown v2;\n";
            if (rawMessage.ForwardOrigin != null)
            {
                switch (rawMessage.ForwardOrigin)
                {
                    case MessageOriginChannel messageOriginChannel:
                        content +=
                            $"User \"{CompoundUserName(rawMessage.From)}\" forwarded message from \"{messageOriginChannel.Chat.Title}\"(@{messageOriginChannel.Chat.Username})\n";
                        break;

                    case MessageOriginChat messageOriginChat:
                        content +=
                            $"User \"{CompoundUserName(rawMessage.From)}\" forwarded message from \"{messageOriginChat.SenderChat.Title}\"(@{messageOriginChat.SenderChat.Username}))\n";

                        break;

                    case MessageOriginHiddenUser messageOriginHiddenUser:
                        content +=
                            $"User \"{CompoundUserName(rawMessage.From)}\" forwarded message from a hidden user with name \"{messageOriginHiddenUser.SenderUserName}\"\n";
                        break;

                    case MessageOriginUser messageOriginUser:
                        content +=
                            $"User \"{CompoundUserName(rawMessage.From)}\" forwarded message from a user with name \"{CompoundUserName(messageOriginUser.SenderUser)}\"\n";
                        break;
                }
            }

            var replyTo = rawMessage.ReplyToMessage?.Text ?? rawMessage.ReplyToMessage?.Caption;
            if (!string.IsNullOrEmpty(replyTo) && replyTo.Length > 0)
            {
                content += $"Content of forwarded message: \"{replyTo}\"\n";
            }

            if (rawMessage.ReplyToMessage?.Photo != null)
            {
                content +=
                    $"{Strings.AttachedImage}: \"{await PhotoToLink(rawMessage.ReplyToMessage.Photo).ConfigureAwait(false)}\"";
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

        [GeneratedRegex("[^a-zA-Z0-9_\\s-]")]
        private static partial Regex WrongNameSymbolsRegexpCreator();
    }
}