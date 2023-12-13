using System.Collections.Concurrent;
using System.Globalization;
using VivyAI.Interfaces;
using VivyAI.MessageCallbacks;

namespace VivyAI
{
    internal static class App
    {
        public static ConcurrentDictionary<string, AppVisitor> visitors;
        public static ConcurrentDictionary<string, IChat> chatById;

        private static Func<string, IOpenAI> openAIFactory;
        private static IMessanger messanger;
        private static IChatCommandProcessor commandProcessor;
        private static Dictionary<string, IMessageCallback> callbacks;

        private const string vivyExceptions = "vivyExceptions.txt";

        public static async Task Main()
        {
            Initialize();
            await Task.Delay(-1);
        }

        private static void Initialize()
        {
            var openAIToken = Environment.GetEnvironmentVariable("OPENAI_TOKEN");
            var telegramBotKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_KEY");
            var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");

            visitors = new ConcurrentDictionary<string, AppVisitor>();
            chatById = new ConcurrentDictionary<string, IChat>();

            callbacks = new Dictionary<string, IMessageCallback>();
            RegisterCallback(new CancelCallback(id => chatById[id]));
            RegisterCallback(new StopCallback(id => chatById[id]));
            RegisterCallback(new RegenerateCallback(id => chatById[id]));
            RegisterCallback(new ContinueCallback(id => chatById[id]));

            openAIFactory = (chatId) => new OpenAI(chatId, openAIToken);
            messanger = new Messanger(telegramBotKey, adminId, HandleMessage, HandleMessageCallback);
            commandProcessor = new ChatCommandProcessor(messanger.IsAdmin);
        }

        private static void RegisterCallback(IMessageCallback callback)
        {
            callbacks.Add(callback.Id.name, callback);
        }

        private static async void HandleMessage(KeyValuePair<string, IChatMessage> packedMessage)
        {
            var chatId = packedMessage.Key;
            var chat = chatById.GetOrAdd(chatId, (_) => new Chat(chatId, openAIFactory(chatId), messanger));
            try
            {
                await chat.LockAsync(IChat.stopCode);
                var message = packedMessage.Value;
                bool isCommandDone = commandProcessor.ExecuteIfChatCommand(chatId, message);
                if (isCommandDone)
                {
                    return;
                }

                await chat.DoResponseToMessage(message);
            }
            catch (Exception e)
            {
                LogException(e);
                _ = messanger.SendMessage(chatId, new ChatMessage(Strings.SomethingGoesWrong));
            }
            finally
            {
                chat.Unlock();
            }
        }

        private static async void HandleMessageCallback(KeyValuePair<string, CallbackCallId> packedCallbackCall)
        {
            var chatId = packedCallbackCall.Value.userId;
            if (chatById.TryGetValue(chatId, out IChat chat))
            {
                try
                {
                    var callback = callbacks[packedCallbackCall.Key];
                    await chat.LockAsync(callback.LockCode);
                    callback.Run(packedCallbackCall.Value);
                }
                catch (Exception e)
                {
                    LogException(e);
                    _ = messanger.SendMessage(chatId, new ChatMessage(Strings.SomethingGoesWrong));
                }
                finally
                {
                    chat.Unlock();
                }
            }
        }

        public static void LogException(Exception e, bool sendAdmin = true)
        {
            Console.WriteLine($"{e.Message}\n{e.StackTrace}");

            using var writer = new StreamWriter(vivyExceptions, true);
            writer.WriteLine("Exception Date: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("Exception Message: " + e.Message);
            writer.WriteLine("Stack Trace: " + e.StackTrace);
            writer.WriteLine(new string('-', 42));

            if (sendAdmin)
            {
                messanger.NotifyAdmin($"{e.Message}\n{e.StackTrace}");
            }
        }

        public static void SendAppMessage(string chatId, IChatMessage message)
        {
            _ = messanger.SendMessage(chatId, message);
        }
    }
}