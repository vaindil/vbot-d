using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using VainBot.Classes.YouTube;
using VainBot.Services;

namespace VainBot.Modules
{
    [Group("youtube")]
    [Alias("yt")]
    public class YouTubeModule : ModuleBase
    {
        private readonly YouTubeService _ytSvc;

        public YouTubeModule(YouTubeService ytSvc)
        {
            _ytSvc = ytSvc;
        }

        [Command]
        [Alias("help")]
        public async Task Help()
        {
            await ReplyAsync("Handles YouTube channel checking. Must have the \"Manage Server\" permission to list/add/remove.\n" +
                "\n" +
                "**NOTE:** To add a mention of `@everyone` or `@here` to a message, use EVERYONE or HERE in all caps. The bot will " +
                "internally replace those so that people are only mentioned when it matters.\n" +
                "\n" +
                "`!youtube list`: Lists all YouTube channels being checked\n\n" +
                "`!youtube add url channel is_deleted message`: Adds a channel to check. The `url` parameter should be the URL of the channel page, " +
                "for example `https://www.youtube.com/user/example_user` or `https://www.youtube.com/channel/UCv8TjL-bZvShlWzXXXXXXXX`. " +
                "The `is_deleted` parameter should be 1 if you want to delete previous announcements when a new one is posted, or any other number " +
                "to keep them forever.\n\n" +
                "`!youtube remove id`: Removes the channel with the given ID. Use `!youtube list` to get the ID.");
        }

        [Command("list")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        public async Task List()
        {
            var channels = _ytSvc.GetChannelsByGuild(Context.Guild.Id);
            var reply = "";
            var multiMessage = false;

            foreach (var c in channels)
            {
                var dChannel = (SocketTextChannel)await Context.Guild.GetChannelAsync((ulong)c.DiscordChannelId);
                var discordMessageToPost = c.DiscordMessageToPost.Replace("`", @"\`");
                reply += $"{c.Id}: `{c.Username}` in {dChannel?.Mention ?? "(nonexistent channel)"} `{discordMessageToPost}`";

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
                reply = "No YouTube channels are being checked on this server.";

            await ReplyAsync(reply);
        }

        [Command("add")]
        [Alias("create")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string url, ITextChannel channel, int isDeleted, [Remainder]string message)
        {
            if (url.Length > 80)
            {
                await ReplyAsync("YouTube URLs can't be that long. Try again.");
                return;
            }

            if (channel == null)
            {
                await ReplyAsync("Channel must be specified.");
                return;
            }

            if (message?.Length > 1500)
            {
                await ReplyAsync("Message must be 1500 characters or fewer.");
                return;
            }

            var ytChannel = new YouTubeChannelToCheck
            {
                DiscordChannelId = (long)channel.Id,
                DiscordGuildId = (long)Context.Guild.Id,
                DiscordMessageToPost = message?.Replace("EVERYONE", "@everyone").Replace("HERE", "@here"),
                IsDeleted = isDeleted == 1
            };

            ytChannel = await _ytSvc.FillInChannelInformationAsync(url, ytChannel);
            if (ytChannel == null)
            {
                await ReplyAsync("The provided URL is invalid.");
                return;
            }

            string response;

            if (await _ytSvc.AddChannelAsync(ytChannel))
            {
                response = $"Added {ytChannel.Username} in {channel.Mention}";
                if (!string.IsNullOrEmpty(message))
                {
                    response += $" with message `{message}`";
                }

                response += ".";
            }
            else
            {
                response = "Unspecified error adding the YouTube channel.";
            }

            await ReplyAsync(response);
        }

        [Command("remove")]
        [Alias("delete")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(int id)
        {
            var ytChannels = _ytSvc.GetChannelsByGuild(Context.Guild.Id);
            var ytChannel = ytChannels.Find(c => c.Id == id);

            if (ytChannel == null)
            {
                await ReplyAsync("That channel doesn't exist. Use `!youtube list` to view channels being checked on this server.");
                return;
            }

            await _ytSvc.RemoveChannelByIdAsync(id);

            var mention = "(nonexistent channel)";
            var channel = (ITextChannel)await Context.Guild.GetChannelAsync((ulong)ytChannel.DiscordChannelId);
            if (channel != null)
                mention = channel.Mention;

            await ReplyAsync($"YouTube checking for {ytChannel.Username} in {mention} has been disabled.");
        }
    }
}
