using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
            await ReplyAsync("Handles Twitch stream checking. Must be a server administrator to list/add/remove.\n" +
                "\n" +
                "**NOTE:** To add a mention of `@everyone` or `@here` to a message, use EVERYONE or HERE in all caps. The bot will " +
                "internally replace those so that people are only mentioned when it matters.\n" +
                "\n" +
                "`!twitch id username`: Gets the Twitch ID for the user with the provided username.\n" +
                "`!twitch list`: Lists all streams being checked.\n" +
                "`!twitch add twitch_username channel message`: Adds a new stream to check.\n" +
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
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task List([Remainder]string unused = null)
        {
            var streams = _twitchSvc.GetStreamsByGuild(Context.Guild.Id);
            var reply = "";

            foreach (var s in streams)
            {
                var channel = (SocketTextChannel)await Context.Guild.GetChannelAsync((ulong)s.ChannelId);
                reply += $"{s.Id}: {s.Username} {channel?.Mention} `{s.MessageToPost}`\n";

                if (reply.Length >= 1700)
                {
                    reply.TrimEnd('\\', 'n');

                    await ReplyAsync(reply);
                    reply = "";
                }
            }

            reply.TrimEnd('\\', 'n');

            if (reply?.Length == 0)
                reply = "No streams are being checked on this server.";

            await ReplyAsync(reply);
        }

        [Command("add")]
        [Alias("create")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Add(string twitchUsername, ITextChannel channel, [Remainder]string message)
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

            await _twitchSvc.AddStreamAsync(new TwitchStreamToCheck
            {
                TwitchId = id,
                Username = displayName,
                MessageToPost = message.Replace("EVERYONE", "@everyone").Replace("HERE", "@here"),
                ChannelId = (long)channel.Id,
                GuildId = (long)Context.Guild.Id,
                IsEmbedded = false,
                IsDeleted = true
            });

            await ReplyAsync($"Added {displayName} in {channel.Mention} with message `{message}`.");
        }

        [Command("remove")]
        [Alias("delete")]
        [RequireUserPermission(GuildPermission.Administrator)]
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

            await ReplyAsync($"Stream checking for {stream.Username} in {mention} has been removed.");
        }
    }
}
