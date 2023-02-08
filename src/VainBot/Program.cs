using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using VainBot.Infrastructure;
using VainBot.Services;

namespace VainBot
{
    public class Program
    {
        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private readonly DiscordRestClient _restClient = new();
        private IConfiguration _config;
        private bool _isDev;

        public async Task MainAsync()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();

            _isDev = IsDebug();

            _client = new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Warning,
                    AlwaysDownloadUsers = true,
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
                });

            var services = ConfigureServices();
            await services.GetRequiredService<InteractionHandler>().InitializeAsync();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            _client.Ready += async () =>
            {
                services.GetRequiredService<ILogger<Program>>().LogInformation("Ready event fired");
                await services.GetRequiredService<ReminderService>().InitializeAsync();

                if (!_isDev)
                {
                    await services.GetRequiredService<TwitchService>().InitializeAsync();
                    await services.GetRequiredService<YouTubeService>().InitializeAsync();
                    // await services.GetRequiredService<TwitterService>().InitializeAsync();
                }
            };

            await _restClient.LoginAsync(TokenType.Bot, _config["discord_api_token"]);

            await _client.LoginAsync(TokenType.Bot, _config["discord_api_token"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private IServiceProvider ConfigureServices()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VainBotDiscord", "2.0"));
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var googleYtSvc = new Google.Apis.YouTube.v3.YouTubeService(new BaseClientService.Initializer
            {
                ApplicationName = "VainBotDiscord",
                ApiKey = _config["youtube_api_key"]
            });

            return new ServiceCollection()
                .Configure<Configs.TwitterConfig>(_config.GetSection("Twitter"))
                .Configure<Configs.TranslationConfig>(_config.GetSection("Translation"))
                .Configure<Configs.TwitchBotRestartConfig>(_config.GetSection("TwitchBotRestart"))
                .AddSingleton(_client)
                .AddSingleton(_restClient)
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<TwitchService>()
                .AddSingleton<YouTubeService>()
                .AddSingleton<TwitterService>()
                .AddSingleton<ReminderService>()
                .AddSingleton(httpClient)
                .AddSingleton(googleYtSvc)
                .AddLogging(o =>
                {
                    o.AddConsole();
                    o.AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Warning);
                    o.AddFilter(DbLoggerCategory.Infrastructure.Name, LogLevel.Warning);
                })
                .Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TimedLogger<>)))
                .AddSingleton(_config)
                .AddDbContext<VbContext>(o => o.UseNpgsql(_config["connection_string"]), ServiceLifetime.Transient)
                .BuildServiceProvider();
        }

        public static bool IsDebug()
        {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
        }
    }
}
