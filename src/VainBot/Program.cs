using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
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
        private IConfiguration _config;
        private bool _isDev;

        public async Task MainAsync()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();

            _isDev = Environment.GetEnvironmentVariable("VB_DEV") != null;

            _client = new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Warning
                });

            var services = ConfigureServices();
            services.GetRequiredService<ActionChannelGuard>();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

            _client.Ready += async () =>
            {
                await services.GetRequiredService<UserService>().InitializeAsync();
                await services.GetRequiredService<TwitterService>().InitializeAsync();

                if (!_isDev)
                {
                    await services.GetRequiredService<TwitchActionsService>().InitializeAsync();
                    await services.GetRequiredService<ReminderService>().InitializeAsync();
                    await services.GetRequiredService<TwitchService>().InitializeAsync();
                    await services.GetRequiredService<YouTubeService>().InitializeAsync();
                }
            };

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
                .Configure<Configs.FitzyConfig>(_config.GetSection("Fitzy"))
                .Configure<Configs.TwitchBotRestartConfig>(_config.GetSection("TwitchBotRestart"))
                .AddSingleton(_client)
                .AddSingleton(new InteractiveService(_client))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<TwitchService>()
                .AddSingleton<YouTubeService>()
                .AddSingleton<TwitterService>()
                .AddSingleton<ReminderService>()
                .AddSingleton<UserService>()
                .AddSingleton<TwitchActionsService>()
                .AddSingleton<ActionChannelGuard>()
                .AddSingleton(httpClient)
                .AddSingleton(googleYtSvc)
                .AddSingleton(new Random())
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
    }
}
