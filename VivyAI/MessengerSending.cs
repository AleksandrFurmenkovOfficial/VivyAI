using RxTelegram.Bot.Interface.BaseTypes.Enums;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Attachments;
using RxTelegram.Bot.Interface.BaseTypes.Requests.Messages;
using System.Globalization;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal sealed partial class Messenger : IMessenger
    {
        const ParseMode cparseMode = ParseMode.Markdown;
        const ParseMode cparseModeFallback = ParseMode.HTML;

        public Task<bool> DeleteMessage(string chatId, string messageId)
        {
            return bot.DeleteMessage(new DeleteMessage
            {
                ChatId = Utils.StrToLong(chatId),
                MessageId = Utils.StrToInt(messageId)
            });
        }

        public async Task<string> SendMessage(string chatId, IChatMessage message, IList<ActionId> messageActionIds = null)
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

                var sentMessage = await bot.SendMessage(sendMessageRequest).ConfigureAwait(false);
                return sentMessage.MessageId.ToString(CultureInfo.InvariantCulture);
            };

            try
            {
                return (await sendMessageInternal(cparseMode).ConfigureAwait(false));
            }
            catch
            {
                return (await sendMessageInternal(cparseModeFallback).ConfigureAwait(false));
            }
        }

        public async Task EditTextMessage(string chatId, string messageId, string newContent, IList<ActionId> messageActionIds = null)
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

                await bot.EditMessageText(editMessageRequest).ConfigureAwait(false);
            };

            try
            {
                await editTextMessageInternal(cparseMode).ConfigureAwait(false);
            }
            catch
            {
                await editTextMessageInternal(cparseModeFallback).ConfigureAwait(false);
            }
        }

        public async Task<string> SendPhotoMessage(string chatId, Uri imageUrl, string caption, IList<ActionId> messageActionIds = null)
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

                return (await bot.SendPhoto(sendPhotoMessage).ConfigureAwait(false)).MessageId.ToString(CultureInfo.InvariantCulture);
            };

            try
            {
                using (var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl).ConfigureAwait(false))
                {
                    return await sendPhotoMessageInternal(cparseMode, imageStream).ConfigureAwait(false);
                }
            }
            catch
            {
                using (var imageStream = await Utils.GetStreamFromUrlAsync(imageUrl).ConfigureAwait(false))
                {
                    return await sendPhotoMessageInternal(cparseModeFallback, imageStream).ConfigureAwait(false);
                }
            }
        }

        public async Task EditMessageCaption(string chatId, string messageId, string caption, IList<ActionId> messageActionIds = null)
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

                await bot.EditMessageCaption(editCaptionRequest).ConfigureAwait(false);
            };

            try
            {
                await editMessageCaptionInternal(cparseMode).ConfigureAwait(false);
            }
            catch
            {
                await editMessageCaptionInternal(cparseModeFallback).ConfigureAwait(false);
            }
        }
    }
}