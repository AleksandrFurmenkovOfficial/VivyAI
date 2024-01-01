using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using VivyAi.Implementation.ChatCommands;
using VivyAi.Implementation.ChatMessageActions;
using VivyAi.Interfaces;

namespace VivyAi.Implementation
{
    internal static class App
    {
        [RequiresDynamicCode(
            "Calls Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider()")]
        public static Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += ExceptionHandler.GlobalExceptionHandler;

            var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                throw new InvalidOperationException("The environment variable 'OPENAI_API_KEY' is not set.");
            }

            var telegramBotKey = Environment.GetEnvironmentVariable("TELEGRAM_BOT_KEY");
            if (string.IsNullOrEmpty(telegramBotKey))
            {
                throw new InvalidOperationException("The environment variable 'TELEGRAM_BOT_KEY' is not set.");
            }

            var adminUserId = Environment.GetEnvironmentVariable("TELEGRAM_ADMIN_USER_ID") ?? "";

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, openAiApiKey, telegramBotKey, adminUserId);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var chatProcessor = serviceProvider.GetRequiredService<IChatProcessor>();
            return chatProcessor.Run();
        }

        private static void ConfigureServices(IServiceCollection services, string openAiApiKey, string telegramBotKey,
            string adminUserId)
        {
            services.AddSingleton(new ConcurrentDictionary<string, IAppVisitor>());
            services.AddSingleton(new ConcurrentDictionary<string, ConcurrentDictionary<string, ActionId>>());
            services.AddSingleton<IAdminChecker, AdminChecker>(_ => new AdminChecker(adminUserId));
            services.AddSingleton<IAiAgentFactory, AiAgentFactory>(_ => new AiAgentFactory(openAiApiKey, new OpenAiImagePainter(openAiApiKey),
                new OpenAiImageDescriptor(openAiApiKey)));
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
                    provider.GetRequiredService<ConcurrentDictionary<string, ConcurrentDictionary<string, ActionId>>>(),
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