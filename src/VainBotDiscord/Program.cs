using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VainBotDiscord
{
    public class Program
    {
        static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        DiscordSocketClient _client;
        bool isDev;

        public async Task MainAsync()
        {
            // check whether running in dev mode
            isDev = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VAINBOT_ISDEV"));

            // the TTSTemp directory is used by the TTS audio command and needs to exist
            Directory.CreateDirectory("TTSTemp");

            VerifyEnvironmentVariables();

            var discordApiToken = Environment.GetEnvironmentVariable("DISCORD_API_TOKEN");

            _client = new DiscordSocketClient();
            await _client.LoginAsync(TokenType.Bot, discordApiToken);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        /// <summary>
        /// Verifies that all required environment variables are set
        /// </summary>
        void VerifyEnvironmentVariables()
        {
            foreach (var envVar in requiredEnvVars)
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                    throw new ArgumentNullException(envVar, "Missing environment variable");
            }
        }

        static readonly List<string> requiredEnvVars = new List<string>
        {
            "DISCORD_API_TOKEN",
            "TWITCH_API_TOKEN",
            "YOUTUBE_API_KEY",
            "TWITTER_CONSUMER_KEY",
            "TWITTER_CONSUMER_SECRET",
            "TWITTER_ACCESS_TOKEN",
            "TWITTER_ACCESS_TOKEN_SECRET",
            "IMGUR_CLIENT_ID"
        };
    }
}
