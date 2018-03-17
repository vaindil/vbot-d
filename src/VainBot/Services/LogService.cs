using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rollbar;
using System;
using System.Threading.Tasks;

namespace VainBot.Services
{
    public class LogService
    {
        readonly Configs.RollbarConfig _config;

        readonly DiscordSocketClient _discord;
        readonly CommandService _commands;

        public LogService(
            IOptions<Configs.RollbarConfig> options,
            DiscordSocketClient discord,
            CommandService commands,
            ILoggerFactory loggerFactory)
        {
            _config = options.Value;

            _discord = discord;
            _commands = commands;

            _discord.Log += OnLog;
            _commands.Log += OnLog;
        }

        public async Task LogExceptionAsync(Exception ex)
        {
            await Task.Run(async () =>
            {
                if (_config.UseRollbar)
                {
                    LogRollbarException(ex);
                    return;
                }

                await LogConsoleExceptionAsync(ex);
            });
        }

        public async Task LogMessageAsync(LogSeverity severity, string msg)
        {
            await Task.Run(async () =>
            {
                if (_config.UseRollbar)
                {
                    LogRollbarMessage(severity, msg);
                    return;
                }

                await LogConsoleMessageAsync(severity, msg);
            });
        }

        void LogRollbarException(Exception ex)
        {
            RollbarLocator.RollbarInstance.Critical(ex);
        }

        Task LogConsoleExceptionAsync(Exception ex)
        {
            return Console.Out.WriteLineAsync(
                $"{DateTime.UtcNow.ToString("yy-MM-dd hh:mm:ss")}: [Critical] {ex.Source}: " +
                (ex?.ToString() ?? ex.Message));
        }

        void LogRollbarMessage(LogSeverity severity, string msg)
        {
            var errorLevel = ErrorLevelFromSeverity(severity);
            if (!errorLevel.HasValue)
                return;

            RollbarLocator.RollbarInstance.Log(errorLevel.Value, msg);
        }

        Task LogConsoleMessageAsync(LogSeverity severity, string msg)
        {
            return Console.Out.WriteLineAsync(
                $"{DateTime.UtcNow.ToString("yy-MM-dd hh:mm:ss")}: [{severity}]: {msg}");
        }

        Task OnLog(LogMessage msg)
        {
            return Task.Run(() =>
            {
                if (_config.UseRollbar)
                {
                    DiscordLogRollbar(msg);
                    return Task.CompletedTask;
                }

                return DiscordLogConsoleAsync(msg);
            });
        }

        void DiscordLogRollbar(LogMessage msg)
        {
            var errorLevel = ErrorLevelFromSeverity(msg.Severity);
            if (!errorLevel.HasValue)
                return;

            RollbarLocator.RollbarInstance.Log(
                errorLevel.Value,
                $"{msg.Source}: {msg.Exception?.ToString() ?? msg.Message}");
        }

        Task DiscordLogConsoleAsync(LogMessage msg)
        {
            return Console.Out.WriteLineAsync(
                $"{DateTime.UtcNow.ToString("yy-MM-dd hh:mm:ss")}: [{msg.Severity}] {msg.Source}: " +
                (msg.Exception?.ToString() ?? msg.Message));
        }

        static ErrorLevel? ErrorLevelFromSeverity(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Verbose:
                    return null;

                case LogSeverity.Debug:
                    return ErrorLevel.Debug;

                default:
                    return (ErrorLevel)severity;
            }
        }
    }
}
