using System.Collections.Concurrent;
using System.Globalization;
using VivyAI.Interfaces;
using VivyAI.MessageActions;

namespace VivyAI
{
    internal static class App
    {
        public static ConcurrentDictionary<string, AppVisitor> visitors;
        public static ConcurrentDictionary<string, IChat> chatById;

        private static Func<string, IAIAgent> openAIFactory;
        private static IMessanger messanger;
        private static IChatCommandProcessor commandProcessor;
        private static Dictionary<string, IChatMessageAction> actions;

        private const string vivyExceptions = "vivyExceptions.txt";

        public static async Task Main()
        {
            Initialize();
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static void Initialize()
        {
            var openAIToken = Environment.GetEnvironmentVariable("OPENAI_TOKEN");
            var telegramBotKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_KEY");
            var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");

            visitors = new ConcurrentDictionary<string, AppVisitor>();
            chatById = new ConcurrentDictionary<string, IChat>();

            actions = new Dictionary<string, IChatMessageAction>();
            RegisterAction(new CancelAction(id => chatById[id]));
            RegisterAction(new StopAction(id => chatById[id]));
            RegisterAction(new RegenerateAction(id => chatById[id]));
            RegisterAction(new ContinueAction(id => chatById[id]));

            openAIFactory = (chatId) => new OpenAIAgent(chatId, openAIToken);
            messanger = new Messanger(telegramBotKey, adminId, HandleMessage, HandleMessageAction);
            commandProcessor = new ChatCommandProcessor(messanger.IsAdmin);
        }

        private static void RegisterAction(IChatMessageAction callback)
        {
            actions.Add(callback.GetId.name, callback);
        }

        private static async void HandleMessage(string chatId, IChatMessage message)
        {
            var chat = chatById.GetOrAdd(chatId, (_) => new Chat(chatId, openAIFactory(chatId), messanger));
            try
            {
                await chat.LockAsync(IChat.stopCode).ConfigureAwait(false);
                bool isCommandDone = commandProcessor.ExecuteIfChatCommand(chatId, message);
                if (isCommandDone)
                {
                    return;
                }

                await chat.DoResponseToMessage(message).ConfigureAwait(false);
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

        private static async void HandleMessageAction(KeyValuePair<string, ActionParameters> packedActionCall)
        {
            var chatId = packedActionCall.Value.userId;
            if (chatById.TryGetValue(chatId, out IChat chat))
            {
                try
                {
                    var callback = actions[packedActionCall.Key];
                    await chat.LockAsync(callback.LockCode).ConfigureAwait(false);
                    callback.Run(packedActionCall.Value);
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