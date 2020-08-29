using Discord.Commands;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    [Group("stalk")]
    [Alias("log", "logs")]
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

        private static string GenerateReply(string username, string channel)
        {
            return $"Logs for user {username} in channel {channel} are at the following link. Note that the link will only work " +
                $"for moderators of the channel. https://www.twitch.tv/popout/{channel}/viewercard/{username}";
        }
    }
}
