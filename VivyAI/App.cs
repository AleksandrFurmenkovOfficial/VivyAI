using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading.Tasks;
using VivyAI.Interfaces;

namespace VivyAI
{
    internal class Visitor
    {
        internal bool access;
        internal string who;

        public Visitor(bool access, string who)
        {
            this.access = access;
            this.who = who;
        }
    }

    internal static class App
    {
        internal static string VivyName = "Vivy";

        internal static ConcurrentDictionary<string, Visitor> visitors;
        internal static ConcurrentDictionary<string, IChat> chatById;

        internal static IOpenAI openAI;
        internal static IMessanger messanger;
        internal static IChatCommandProcessor commandProcessor;

        internal static string openAIToken;
        internal static string telegrammBotKey;
        internal static string adminId;

        public static async Task Main(string[] args)
        {
            Initialize(args);
            await Task.Delay(-1).ConfigureAwait(false);
        }

        public static void Initialize(string[] args)
        {
            openAIToken = args[0];
            telegrammBotKey = args[1];
            adminId = args[2];

            visitors = new ConcurrentDictionary<string, Visitor>();
            chatById = new ConcurrentDictionary<string, IChat>();

            openAI = new OpenAI(openAIToken);
            messanger = new Messanger(telegrammBotKey, adminId);

            _ = messanger.Message.Subscribe(HandleMessage);
            commandProcessor = new ChatCommandProcessor(messanger.IsAdmin);
        }

        private static async void HandleMessage(IChatMessage message)
        {
            var chat = chatById.GetOrAdd(message.chatId, (_) => new Chat(openAI, messanger));
            try
            {
                await chat.LockAsync().ConfigureAwait(false);
                bool isCommandDone = await commandProcessor.ExecuteIfChatCommand(message).ConfigureAwait(false);
                if (isCommandDone)
                    return;

                var dateTime = DateTime.Now;
                _ = message.content.Insert(0, $"[{dateTime.ToShortDateString()}|{dateTime.ToShortTimeString()}] ");
                await chat.DoResponseToMessage(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _ = messanger.NotifyAdmin(ex.Message).ConfigureAwait(false);
                _ = messanger.SendMessage(new ChatMessage { chatId = message.chatId, content = Strings.SomethingGoesWrong }).ConfigureAwait(false);
            }
            finally
            {
                chat.Unlock();
            }
        }

        public static void SendAppMessage(IChatMessage message)
        {
            _ = messanger.SendMessage(message).ConfigureAwait(false);
        }
    }
}