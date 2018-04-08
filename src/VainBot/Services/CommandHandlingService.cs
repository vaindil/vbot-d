using Discord;
using Discord.Commands;
using Discord.WebSocket;
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

        public CommandHandlingService(DiscordSocketClient discord, CommandService commands, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;

            _discord.MessageReceived += MessageReceived;
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
            if (!message.HasCharPrefix('!', ref argPos))
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
                }

                await context.Channel.SendMessageAsync($"{context.User.Mention}: {msg}");
            }
        }
    }
}
