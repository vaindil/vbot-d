using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using VainBotDiscord.Classes;
using VainBotDiscord.Services;

namespace VainBotDiscord
{
    public class Program
    {
        public static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        DiscordSocketClient _client;
        IConfiguration _config;

        bool _isDev;

        public async Task MainAsync()
        {
            if (Environment.GetEnvironmentVariable("VAINBOT_ISDEV") != null)
                _isDev = true;

            _client = new DiscordSocketClient();
            _config = BuildConfig();

            var services = ConfigureServices();

            services.GetRequiredService<LogService>();
            await SetUpDB(services.GetRequiredService<VbContext>());
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["discord_api_token"]);
            await _client.StartAsync();

            if (!_isDev)
            {
                await services.GetRequiredService<TwitchService>().InitializeAsync(services);
                await services.GetRequiredService<YouTubeService>().InitializeAsync(services);
                await services.GetRequiredService<TwitterService>().InitializeAsync(services);
            }

            await Task.Delay(-1);
        }

        IServiceProvider ConfigureServices()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Clear();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VainBotDiscord", "2.0"));
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var dbContextOptions = new DbContextOptionsBuilder().UseNpgsql(_config["connection_string"]).Options;

            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<TwitchService>()
                .AddSingleton<YouTubeService>()
                .AddSingleton<TwitterService>()
                .AddSingleton<ReminderService>()
                .AddSingleton(httpClient)
                .AddLogging()
                .AddSingleton<LogService>()
                .AddSingleton(_config)
                .AddDbContext<VbContext>(o => o.UseNpgsql(_config["connection_string"]), ServiceLifetime.Transient)
                .BuildServiceProvider();
        }

        IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }

        // https://stackoverflow.com/a/15228558/1672458
        async Task SetUpDB(VbContext db)
        {
            foreach (var key in typeof(KeyValueKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (key.IsLiteral && !key.IsInitOnly)
                {
                    var val = (string)key.GetRawConstantValue();
                    var kv = await db.FindAsync<KeyValue>(val);
                    if (kv == null)
                    {
                        db.Add(new KeyValue(val, ""));
                        await db.SaveChangesAsync();
                    }
                }
            }
        }
    }
}
