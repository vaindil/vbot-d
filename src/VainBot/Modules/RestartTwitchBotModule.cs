using Discord.Commands;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Threading.Tasks;
using VainBot.Configs;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [FitzyModChannel]
    [FitzyModerator]
    public class RestartTwitchBotModule : ModuleBase
    {
        private readonly TwitchBotRestartConfig _config;

        public RestartTwitchBotModule(IOptions<TwitchBotRestartConfig> options)
        {
            _config = options.Value;
        }

        [Command("restarttwitch")]
        [Alias("twitchrestart")]
        public async Task RestartTwitchBot([Remainder]string _ = null)
        {
            if (_config?.Command == null)
                return;

            using (var process = new Process())
            {
                process.StartInfo.FileName = _config.Command;
                process.StartInfo.Arguments = _config.Arguments;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;

                process.Start();
            }

            await ReplyAsync("Twitch bot restart triggered. Bot should finish restarting in a moment.");
        }
    }
}
