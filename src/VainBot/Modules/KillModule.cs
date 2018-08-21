using Discord.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [FitzyGuild]
    [FitzyModerator]
    public class KillModule : ModuleBase
    {
        private readonly ILogger<KillModule> _logger;

        public KillModule(ILogger<KillModule> logger)
        {
            _logger = logger;
        }

        [Command("kill")]
        [Alias("restart")]
        public async Task Restart()
        {
            _logger.LogCritical($"Bot restart initiated by {Context.User.Username}");

            await ReplyAsync("Bot is now restarting. No message will be sent when it comes back online.");
            Environment.Exit(1);
        }
    }
}
