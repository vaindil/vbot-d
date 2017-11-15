using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using VainBotDiscord.Services;

namespace VainBotDiscord
{
    public class Program
    {
        static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        DiscordSocketClient _client;
        IConfiguration _config;

        public async Task MainAsync()
        {
            // this is used for the TTS commands in the audio module
            if (!Directory.Exists("TTSTemp"))
                Directory.CreateDirectory("TTSTemp");

            _client = new DiscordSocketClient();
            _config = BuildConfig();

            var services = ConfigureServices();
            await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

            await _client.LoginAsync(TokenType.Bot, _config["discord_api_token"]);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        IServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddLogging()
                .AddSingleton<LogService>()
                .AddSingleton(_config)
                .AddDbContext<VbContext>(options => options.UseMySql(_config["connection_string"]))
                .BuildServiceProvider();
        }

        IConfiguration BuildConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build();
        }
    }
}
