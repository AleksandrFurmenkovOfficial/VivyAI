using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using VivyAI.Implementation.ChatCommands;
using VivyAI.Implementation.ChatMessageActions;
using VivyAI.Interfaces;

namespace VivyAI.Implementation
{
    internal static class App
    {
        public static Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler.GlobalExceptionHandler;

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var chatProcessor = serviceProvider.GetRequiredService<IChatProcessor>();
            return chatProcessor.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var telegramBotKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_KEY");
            var adminUserId = Environment.GetEnvironmentVariable("TELEGRAM_ADMIN_USER_ID");

            services.AddSingleton(new ConcurrentDictionary<string, IAppVisitor>());
            services.AddSingleton(new ConcurrentDictionary<string, ActionId>());
            services.AddSingleton<IAdminChecker, AdminChecker>(_ => new AdminChecker(adminUserId));
            services.AddSingleton<IAiAgentFactory, AiAgentFactory>(_ => new AiAgentFactory(openAiApiKey));
            services.AddSingleton<ITelegramBotSource, TelegramBotSource>(_ =>
                new TelegramBotSource(telegramBotKey));
            services.AddSingleton<IMessenger, Messenger>();
            services.AddSingleton<IChatFactory, ChatFactory>();
            services.AddSingleton<IChatCommandProcessor, ChatCommandProcessor>();
            services.AddSingleton<IChatMessageProcessor, ChatMessageProcessor>();
            services.AddSingleton<IChatMessageActionProcessor, ChatMessageActionProcessor>();
            services.AddSingleton<IChatProcessor, ChatProcessor>(provider =>
            {
                var chatProcessor = new ChatProcessor(
                    telegramBotKey,
                    provider.GetRequiredService<ConcurrentDictionary<string, IAppVisitor>>(),
                    provider.GetRequiredService<ConcurrentDictionary<string, ActionId>>(),
                    provider.GetRequiredService<IAdminChecker>(),
                    provider.GetRequiredService<IChatFactory>(),
                    provider.GetRequiredService<IChatMessageProcessor>(),
                    provider.GetRequiredService<IChatMessageActionProcessor>(),
                    provider.GetRequiredService<ITelegramBotSource>());
                return chatProcessor;
            });
        }
    }
}