using Discord.Commands;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [Group("stalk")]
    [Alias("overrustle")]
    public class StalkModule : ModuleBase
    {
        private readonly Regex _validUsername =
            new Regex("^[a-zA-Z0-9_]{1,35}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        [Command]
        [Alias("help")]
        public async Task Help()
        {
            await ReplyAsync("Stalk a Twitch user: `!stalk twitch_username twitch_channel`");
        }

        [Command]
        public async Task Stalk(string username, string channel)
        {
            if (!_validUsername.IsMatch(username))
            {
                await ReplyAsync("Provided username is not a valid Twitch username.");
                return;
            }

            if (!_validUsername.IsMatch(channel))
            {
                await ReplyAsync("Provided channel name is not a valid Twitch username.");
                return;
            }

            await ReplyAsync(GenerateReply(username, channel));
        }

        [Command]
        [FitzyGuild]
        public async Task StalkFitzy(string username)
        {
            if (!_validUsername.IsMatch(username))
            {
                await ReplyAsync("Provided username is not a valid Twitch username.");
                return;
            }

            await ReplyAsync(GenerateReply(username, "fitzyhere"));
        }

        private static string GenerateReply(string username, string channel)
        {
            return $"If user `{username}` has chatted in channel `{channel}`, " +
                $"their logs will be at https://ttv.overrustlelogs.net/{channel}/{username}.";
        }
    }
}
