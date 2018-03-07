using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VainBotDiscord.Services
{
    public class LogService
    {
        readonly DiscordSocketClient _discord;
        readonly CommandService _commands;
        readonly ILoggerFactory _loggerFactory;
        readonly ILogger _discordLogger;
        readonly ILogger _commandsLogger;

        public LogService(DiscordSocketClient discord, CommandService commands, ILoggerFactory loggerFactory)
        {
            _discord = discord;
            _commands = commands;

            _loggerFactory = ConfigureLogging(loggerFactory);
            _discordLogger = _loggerFactory.CreateLogger("discord");
            _commandsLogger = _loggerFactory.CreateLogger("commands");

            _discord.Log += LogDiscord;
            _commands.Log += LogCommand;
        }

        Task LogDiscord(LogMessage message)
        {
            _discordLogger.Log(
                LogLevelFromSeverity(message.Severity),
                0,
                message,
                message.Exception,
                (_1, _2) => message.ToString());
            return Task.CompletedTask;
        }

        Task LogCommand(LogMessage message)
        {
            _commandsLogger.Log(
                LogLevelFromSeverity(message.Severity),
                0,
                message,
                message.Exception,
                (_1, _2) => message.ToString());
            return Task.CompletedTask;
        }

        ILoggerFactory ConfigureLogging(ILoggerFactory factory)
        {
            factory.AddConsole(LogLevel.Warning);
            return factory;
        }

        static LogLevel LogLevelFromSeverity(LogSeverity severity)
            => (LogLevel)(Math.Abs((int)severity - 5));
    }
}
