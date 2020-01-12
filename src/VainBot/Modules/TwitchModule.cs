using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using VainBot.Classes.Twitch;
using VainBot.Services;

namespace VainBot.Modules
{
    [Group("twitch")]
    [Alias("stream")]
    public class TwitchModule : ModuleBase
    {
        readonly TwitchService _twitchSvc;

        public TwitchModule(TwitchService twitchSvc)
        {
            _twitchSvc = twitchSvc;
        }

        [Command]
        [Alias("help")]
        public async Task Help()
        {
            await ReplyAsync("Handles Twitch stream checking. Must have the \"Manage Server\" permission to list/add/remove.\n" +
                "\n" +
                "**NOTE:** To add a mention of `@everyone` or `@here` to a message, use EVERYONE or HERE in all caps. The bot will " +
                "internally replace those so that people are only mentioned when it matters.\n" +
                "\n" +
                "`!twitch id username`: Gets the Twitch ID for the user with the provided username.\n" +
                "`!twitch list`: Lists all streams being checked.\n" +
                "`!twitch add twitch_username channel is_embedded message`: Adds a new stream to check. `is_embedded` should be 1 to add an " +
                "embed, any other number will not embed.\n" +
                "`!twitch remove id`: Removes the stream with the given ID. Use `!twitch list` to get the ID.");
        }

        [Command("id")]
        public async Task Id([Remainder]string username)
        {
            var (id, displayName) = await _twitchSvc.GetUserIdAsync(username);

            if (id == "-1")
            {
                await ReplyAsync(displayName);
                return;
            }

            await ReplyAsync($"**{displayName}**: {id}");
        }

        [Command("list")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task List()
        {
            var streams = _twitchSvc.GetStreamsByGuild(Context.Guild.Id).OrderBy(x => x.Username);
            var reply = "";
            var multiMessage = false;

            foreach (var s in streams)
            {
                var channel = (SocketTextChannel)await Context.Guild.GetChannelAsync((ulong)s.ChannelId);
                var isEmbedded = s.IsEmbedded ? "(embedded) " : "";
                var messageToPost = s.MessageToPost.Replace("`", @"\`");
                reply += $"{s.Id}: `{s.Username}` in {channel?.Mention ?? "(nonexistent channel)"} {isEmbedded}`{messageToPost}`\n";

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
                reply = "No streams are being checked on this server.";

            await ReplyAsync(reply);
        }

        [Command("add")]
        [Alias("create")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string twitchUsername, ITextChannel channel, int isEmbedded, [Remainder]string message)
        {
            if (twitchUsername.Length > 40)
            {
                await ReplyAsync("Twitch usernames aren't that long. Try again.");
                return;
            }

            if (channel == null)
            {
                await ReplyAsync("Channel must be specified.");
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                await ReplyAsync("Message cannot be blank.");
                return;
            }

            if (message.Length > 1500)
            {
                await ReplyAsync("Message must be 1500 characters or fewer.");
                return;
            }

            var (id, displayName) = await _twitchSvc.GetUserIdAsync(twitchUsername);
            if (id == "-1")
            {
                await ReplyAsync(displayName);
                return;
            }

            var success = await _twitchSvc.AddStreamAsync(new TwitchStreamToCheck
            {
                TwitchId = id,
                Username = displayName,
                MessageToPost = message.Replace("EVERYONE", "@everyone").Replace("HERE", "@here"),
                ChannelId = (long)channel.Id,
                GuildId = (long)Context.Guild.Id,
                IsEmbedded = isEmbedded == 1,
                IsDeleted = true
            });

            if (success)
                await ReplyAsync($"Added {displayName} in {channel.Mention} with message `{message}`.");
            else
                await ReplyAsync("An error occurred while trying to add that Twitch stream.");
        }

        [Command("remove")]
        [Alias("delete")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(int id)
        {
            var streams = _twitchSvc.GetStreamsByGuild(Context.Guild.Id);
            var stream = streams.Find(s => s.Id == id);

            if (stream == null)
            {
                await ReplyAsync("That stream doesn't exist. Use `!twitch list` to view streams for this server.");
                return;
            }

            await _twitchSvc.RemoveStreamByIdAsync(id);

            var mention = "(nonexistent channel)";
            var channel = (ITextChannel)await Context.Guild.GetChannelAsync((ulong)stream.ChannelId);
            if (channel != null)
                mention = channel.Mention;

            await ReplyAsync($"Stream checking for {stream.Username} in {mention} has been disabled.");
        }
    }
}
