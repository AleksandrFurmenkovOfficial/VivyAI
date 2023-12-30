using System.Collections.Concurrent;
using System.Globalization;
using RxTelegram.Bot;
using RxTelegram.Bot.Interface.BaseTypes;
using RxTelegram.Bot.Interface.BaseTypes.Enums;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Attachments;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using VivyAI.Interfaces;

namespace VivyAI.Implementation
{
    internal sealed class Messenger : IMessenger
    {
        private const ParseMode MainParseMode = ParseMode.Markdown;
        private const ParseMode FallbackParseMode = ParseMode.HTML;

        private readonly ConcurrentDictionary<string, ActionId> callbacksMapping;
        private readonly ITelegramBotSource telegramBotSource;

        public Messenger(ConcurrentDictionary<string, ActionId> callbacksMapping, ITelegramBotSource telegramBotSource)
        {
            this.callbacksMapping = callbacksMapping;
            this.telegramBotSource = telegramBotSource;
        }

        private ITelegramBot Bot => telegramBotSource.GetTelegramBot();

        public Task<bool> DeleteMessage(string chatId, string messageId)
        {
            return Bot.DeleteMessage(new DeleteMessage
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.StrToInt(messageId)
            });
        }

        public async Task<string> SendMessage(string chatId, IChatMessage message,
            IEnumerable<ActionId> messageActionIds = null)
        {
            var sendMessageInternal = async (ParseMode parseMode) =>
            {
                var sendMessageRequest = new SendMessage
                {
                    ChatId = Utils.StrToLong(chatId),
                    Text = message.Content,
                    ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds),
                    ParseMode = parseMode
                };

                var sentMessage = await Bot.SendMessage(sendMessageRequest).ConfigureAwait(false);
                return sentMessage.MessageId.ToString(CultureInfo.InvariantCulture);
            };

            async Task<string> reTry(int tryCount = 3)
            {
                try
                {
                    return await sendMessageInternal(MainParseMode).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        return await sendMessageInternal(FallbackParseMode).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            return await reTry(tryCount - 1).ConfigureAwait(false);
                        }

                        throw;
                    }
                }
            }

            return await reTry().ConfigureAwait(false);
        }

        public async Task EditTextMessage(string chatId, string messageId, string newContent,
            IEnumerable<ActionId> messageActionIds = null)
        {
            var editTextMessageInternal = async (ParseMode parseMode) =>
            {
                var editMessageRequest = new EditMessageText
                {
                    ChatId = Utils.StrToLong(chatId),
                    MessageId = Utils.StrToInt(messageId),
                    Text = (string)newContent.Clone(),
                    ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds),
                    ParseMode = parseMode
                };

                await Bot.EditMessageText(editMessageRequest).ConfigureAwait(false);
            };

            async Task reTry(int tryCount = 3)
            {
                try
                {
                    await editTextMessageInternal(MainParseMode).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await editTextMessageInternal(FallbackParseMode).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            await reTry(tryCount - 1).ConfigureAwait(false);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            await reTry().ConfigureAwait(false);
        }

        public async Task<string> SendPhotoMessage(string chatId, Uri imageUrl, string caption,
            IEnumerable<ActionId> messageActionIds = null)
        {
            var sendPhotoMessageInternal = async (ParseMode parseMode, Stream imageStream) =>
            {
                var sendPhotoMessage = new SendPhoto
                {
                    ChatId = Utils.StrToLong(chatId),
                    Photo = new InputFile(imageStream),
                    Caption = caption,
                    ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds),
                    ParseMode = parseMode
                };

                return (await Bot.SendPhoto(sendPhotoMessage).ConfigureAwait(false)).MessageId.ToString(CultureInfo
                    .InvariantCulture);
            };

            async Task<string> reTry(int tryCount = 3)
            {
                try
                {
                    using (var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl).ConfigureAwait(false))
                    {
                        return await sendPhotoMessageInternal(MainParseMode, imageStream).ConfigureAwait(false);
                    }
                }
                catch
                {
                    try
                    {
                        using (var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl).ConfigureAwait(false))
                        {
                            return await sendPhotoMessageInternal(FallbackParseMode, imageStream).ConfigureAwait(false);
                        }
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            return await reTry(tryCount - 1).ConfigureAwait(false);
                        }

                        throw;
                    }
                }
            }

            return await reTry().ConfigureAwait(false);
        }

        public async Task EditMessageCaption(string chatId, string messageId, string caption,
            IEnumerable<ActionId> messageActionIds = null)
        {
            var editMessageCaptionInternal = async (ParseMode parseMode) =>
            {
                var editCaptionRequest = new EditMessageCaption
                {
                    ChatId = Utils.StrToLong(chatId),
                    MessageId = Utils.StrToInt(messageId),
                    Caption = caption,
                    ReplyMarkup = GetInlineKeyboardMarkup(messageActionIds),
                    ParseMode = parseMode
                };

                await Bot.EditMessageCaption(editCaptionRequest).ConfigureAwait(false);
            };

            async Task reTry(int tryCount = 3)
            {
                try
                {
                    await editMessageCaptionInternal(MainParseMode).ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await editMessageCaptionInternal(FallbackParseMode).ConfigureAwait(false);
                    }
                    catch
                    {
                        if (tryCount > 0)
                        {
                            await reTry(tryCount - 1).ConfigureAwait(false);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            await reTry().ConfigureAwait(false);
        }

        private InlineKeyboardMarkup GetInlineKeyboardMarkup(IEnumerable<ActionId> messageActionIds)
        {
            InlineKeyboardMarkup inlineKeyboardMarkup = null;
            var messageActionIdsList = messageActionIds?.ToList();
            if (messageActionIdsList != null && messageActionIdsList.Any())
            {
                callbacksMapping.Clear();
                var buttons = new List<InlineKeyboardButton>();
                foreach (var callbackId in messageActionIdsList)
                {
                    var token = Guid.NewGuid().ToString();
                    _ = callbacksMapping.TryAdd(token, callbackId);
                    buttons.Add(new InlineKeyboardButton
                    {
                        Text = callbackId.Name,
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
    }
}