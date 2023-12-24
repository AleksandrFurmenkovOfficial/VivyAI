using System.Collections.Concurrent;
using System.Globalization;
using VivyAI.Interfaces;
using VivyAI.MessageActions;

namespace VivyAI
{
    internal static class App
    {
        private const string vivyExceptions = "vivyExceptions.txt";

        private static ConcurrentDictionary<string, AppVisitor> visitors;
        private static ConcurrentDictionary<string, IChat> chatById;
        private static Func<string, IAIAgent> openAIFactory;
        private static IMessenger Messenger;
        private static IChatCommandProcessor commandProcessor;
        private static IDictionary<string, IChatMessageAction> actions;

        public static async Task Main()
        {
            Initialize();
            await Task.Delay(-1).ConfigureAwait(false);
        }

        private static void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(GlobalExceptionHandler);

            var openAIToken = Environment.GetEnvironmentVariable("OPENAI_TOKEN");
            var telegramBotKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_KEY");
            var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");

            visitors = new ConcurrentDictionary<string, AppVisitor>();
            chatById = new ConcurrentDictionary<string, IChat>();

            openAIFactory = (chatId) => new OpenAIAgent(chatId, openAIToken);
            Messenger = new Messenger(telegramBotKey, adminId, HandleMessage, HandleMessageAction, visitors);
            commandProcessor = new ChatCommandProce(Messenger.IsAdmin, visitors, chatById, Messenger);

            RegisterActions();
        }

        private static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            LogException((Exception)e.ExceptionObject);
        }

        private static void RegisterActions()
        {
            static void RegisterAction(IChatMessageAction callback)
            {
                actions.Add(callback.GetId.name, callback);
            }

            actions = new Dictionary<string, IChatMessageAction>();
            static IChat chatGetter(string id) => chatById[id];
            RegisterAction(new CancelAction(chatGetter));
            RegisterAction(new StopAction(chatGetter));
            RegisterAction(new RegenerateAction(chatGetter));
            RegisterAction(new ContinueAction(chatGetter));
            RegisterAction(new RetryAction(chatGetter, Messenger));
        }

        private static async Task HandleMessage(string chatId, IChatMessage message)
        {
            var chat = chatById.GetOrAdd(chatId, (_) => new Chat(chatId, openAIFactory(chatId), Messenger));
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
                _ = Messenger.SendMessage(chatId, new ChatMessage(Strings.SomethingGoesWrong), new List<ActionId> { new ActionId(RetryAction.Name) });
            }
            finally
            {
                chat.Unlock();
            }
        }

        private static async Task HandleMessageAction(KeyValuePair<string, ActionParameters> packedActionCall)
        {
            var chatId = packedActionCall.Value.userId;
            if (!chatById.TryGetValue(chatId, out IChat chat))
                return;

            try
            {
                var callback = actions[packedActionCall.Key];
                await chat.LockAsync(callback.LockCode).ConfigureAwait(false);
                callback.Run(packedActionCall.Value);
            }
            catch (Exception e)
            {
                LogException(e);
                _ = Messenger.SendMessage(chatId, new ChatMessage(Strings.SomethingGoesWrong), new List<ActionId> { new ActionId(RetryAction.Name) });
            }
            finally
            {
                chat.Unlock();
            }
        }

        public static void LogException(Exception e, bool sendAdmin = true)
        {
            Console.WriteLine($"{e.Message}\n{e.StackTrace}");

            using (var writer = new StreamWriter(vivyExceptions, true))
            {
                writer.WriteLine("Exception Date: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
                writer.WriteLine("Exception Message: " + e.Message);
                writer.WriteLine("Stack Trace: " + e.StackTrace);
                writer.WriteLine(new string('-', 42));
            }

            if (sendAdmin)
            {
                Messenger.NotifyAdmin($"{e.Message}\n{e.StackTrace}");
            }
        }
    }
}