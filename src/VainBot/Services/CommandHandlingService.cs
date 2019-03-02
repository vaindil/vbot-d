using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace VainBot.Services
{
    public class CommandHandlingService
    {
        readonly DiscordSocketClient _discord;
        readonly CommandService _commands;
        readonly IServiceProvider _provider;
        readonly ILogger<CommandHandlingService> _logger;

        readonly char _prefix;

        public CommandHandlingService(DiscordSocketClient discord, CommandService commands,
            IServiceProvider provider, ILogger<CommandHandlingService> logger)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _logger = logger;

            _discord.MessageReceived += MessageReceived;

            if (Environment.GetEnvironmentVariable("VB_DEV") != null)
                _prefix = '+';
            else
                _prefix = '!';
        }

        public async Task InitializeAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        }

        async Task MessageReceived(SocketMessage rawMessage)
        {
            // ignore system messages and other bots
            if (!(rawMessage is SocketUserMessage message))
                return;
            if (message.Source != MessageSource.User)
                return;

            int argPos = 0;
            if (!message.HasCharPrefix(_prefix, ref argPos))
                return;

            var context = new SocketCommandContext(_discord, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                var msg = "An unknown error occurred while running that command.";

                switch (result.Error)
                {
                    case CommandError.BadArgCount:
                        msg = "You didn't specify the right number of parameters for that command. " +
                            "It's possible that you don't have permission to use the command you tried to use.";
                        break;

                    case CommandError.MultipleMatches:
                        msg = "Your command matches multiple possible commands.";
                        break;

                    case CommandError.UnmetPrecondition:
                        msg = "You can't use that command.";
                        break;

                    default:
                        _logger.LogError($"[{context.Guild.Name}][{context.Channel.Name}][{context.User.Username}] " +
                            $"Command error: {result.ErrorReason}");
                        break;
                }

                await context.Channel.SendMessageAsync($"{context.User.Mention}: {msg}");
            }
        }
    }
}
