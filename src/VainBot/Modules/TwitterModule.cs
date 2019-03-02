using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using VainBot.Classes.Twitter;
using VainBot.Services;

namespace VainBot.Modules
{
    [Group("twitter")]
    public class TwitterModule : ModuleBase
    {
        private readonly TwitterService _twitterSvc;

        public TwitterModule(TwitterService twitterSvc)
        {
            _twitterSvc = twitterSvc;
        }

        [Command]
        [Alias("help")]
        public async Task Help()
        {
            await ReplyAsync("Handles Twitter timeline checking. Must have the \"Manage Server\" permission to list/add/remove.\n" +
                "\n" +
                "`!twitter list`: Lists all timelines being checked.\n" +
                "`!twitter add twitter_username channel include_RTs`: Adds a new timeline to check. `include_RTs` should be 1 to include RTs, any other number will ignore them.\n" +
                "`!twitter remove id`: Removes the timeline with the given ID. Use `!twitter list` to get the ID.");
        }

        [Command("list")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task List()
        {
            var timelines = _twitterSvc.GetTimelinesByGuild(Context.Guild.Id).OrderBy(x => x.TwitterUsername);
            var reply = "";
            var multiMessage = false;

            foreach (var t in timelines)
            {
                var channel = (SocketTextChannel)await Context.Guild.GetChannelAsync((ulong)t.DiscordChannelId);
                var includeRTs = t.IncludeRetweets ? " (includes RTs)" : "";
                reply += $"{t.Id}: `{t.TwitterUsername}` in {channel?.Mention ?? "(nonexistent channel)"}{includeRTs}\n";

                if (reply.Length >= 1700)
                {
                    reply.TrimEnd('\\', 'n');

                    await ReplyAsync(reply);
                    reply = "";
                    multiMessage = true;
                }
            }

            reply.TrimEnd('\\', 'n');

            if (reply?.Length == 0 && !multiMessage)
                reply = "No timelines are being checked on this server.";

            await ReplyAsync(reply);
        }

        [Command("add")]
        [Alias("create")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string twitterUsername, ITextChannel channel, int includeRTs)
        {
            if (channel == null)
            {
                await ReplyAsync("Channel must be specified");
                return;
            }

            var (twitterId, username) = _twitterSvc.GetUserInfo(twitterUsername);
            if (!twitterId.HasValue)
            {
                await ReplyAsync("Twitter username does not exist.");
                return;
            }

            var success = await _twitterSvc.AddTwitterToCheckAsync(new TwitterToCheck
            {
                TwitterUsername = username,
                TwitterId = twitterId.Value,
                DiscordGuildId = (long)Context.Guild.Id,
                DiscordChannelId = (long)channel.Id,
                IncludeRetweets = includeRTs == 1
            });

            if (success)
                await ReplyAsync($"Added {username} in {channel.Mention}.");
            else
                await ReplyAsync("An error occurred while trying to add that timeline.");
        }

        [Command("remove")]
        [Alias("delete")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(int id)
        {
            var timelines = _twitterSvc.GetTimelinesByGuild(Context.Guild.Id);
            var timeline = timelines.Find(x => x.Id == id);

            if (timeline == null)
            {
                await ReplyAsync("That timeline doesn't exist. Use `!twitter list` to view timelines for this server.");
                return;
            }

            await _twitterSvc.RemoveTwitterToCheckByIdAsync(id);

            var mention = "(nonexistent channel)";
            var channel = (ITextChannel)await Context.Guild.GetChannelAsync((ulong)timeline.DiscordChannelId);
            if (channel != null)
                mention = channel.Mention;

            await ReplyAsync($"Timeline checking for {timeline.TwitterUsername} in {mention} has been disabled.");
        }
    }
}
