using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VainBot.Services;

namespace VainBot.SlashCommandModules
{
    [Group("friendtime", "Display or configure timezone for users in a channel")]
    public class FriendTimeSlashCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly FriendTimeService _friendTimeSvc;
        private readonly DiscordSocketClient _client;
        private readonly ILogger<FriendTimeSlashCommandModule> _logger;

        public FriendTimeSlashCommandModule(
            FriendTimeService friendTimeSvc,
            DiscordSocketClient client,
            ILogger<FriendTimeSlashCommandModule> logger)
        {
            _friendTimeSvc = friendTimeSvc;
            _client = client;
            _logger = logger;
        }

        private ulong EffectiveChannelId => Context.Channel is SocketThreadChannel threadChannel 
            ? threadChannel.ParentChannel.Id 
            : Context.Channel.Id;

        [SlashCommand("now", "Show the local time for everyone in this channel who configured their timezone")]
        [CommandContextType(InteractionContextType.Guild)]
        public async Task FriendTimeNow()
        {
            if (Context.Guild == null)
            {
                await RespondAsync("This command can only be used in a server channel.", ephemeral: true);
                return;
            }

            await DeferAsync();

            var friends = await _friendTimeSvc.GetFriendTimesForChannelAsync(Context.Guild.Id, EffectiveChannelId);

            if (friends == null || friends.Count == 0)
            {
                await FollowupAsync("No one in this channel has configured their timezone yet. Use `/friendtime add` to add yourself!");
                return;
            }

            var lines = new List<string>();
            var sortedFriends = friends.OrderBy(f =>
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(f.Timezone).GetUtcOffset(DateTimeOffset.UtcNow);
                }
                catch
                {
                    return TimeSpan.Zero;
                }
            });

            foreach (var friend in sortedFriends)
            {
                TimeZoneInfo tz;
                try
                {
                    tz = TimeZoneInfo.FindSystemTimeZoneById(friend.Timezone);
                }
                catch
                {
                    _logger.LogWarning($"Invalid timezone '{friend.Timezone}' configured for user {friend.UserId} in channel {friend.ChannelId}.");
                    continue;
                }

                var localTime = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
                
                // Format: h:mm tt (e.g. 3:42 AM or 3:42 PM)
                var timeString = localTime.ToString("h:mm tt");

                // Compact representation using user mention (renders name and enables hover avatar)
                lines.Add($"<@{(ulong)friend.UserId}> • **{timeString}**");
            }

            if (lines.Count == 0)
            {
                await FollowupAsync("No configured timezones could be resolved. Use `/friendtime add` to configure a valid timezone!");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("Friend Times")
                .WithDescription(string.Join("\n", lines))
                .WithColor(Color.Blue)
                .WithFooter("Use /friendtime add to add yourself")
                .Build();

            await FollowupAsync(embed: embed);
        }

        [SlashCommand("add", "Add or update a timezone for a user (only admins can add for other users)")]
        [CommandContextType(InteractionContextType.Guild)]
        public async Task FriendTimeAdd(
            [Summary(description: "IANA Time Zone ID (e.g., Europe/Bucharest, America/New_York)")] string timezone,
            [Summary(description: "The user to add/update (admin only)")] SocketGuildUser user = null)
        {
            if (Context.Guild == null)
            {
                await RespondAsync("This command can only be used in a server channel.", ephemeral: true);
                return;
            }

            var targetUser = user ?? (SocketGuildUser)Context.User;
            var isSelf = targetUser.Id == Context.User.Id;
            var isAdmin = Context.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator;

            if (!isSelf && !isAdmin)
            {
                await RespondAsync("You do not have permission to add or overwrite another user's timezone. Only server administrators can do this.", ephemeral: true);
                return;
            }

            // Validate Time Zone
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                await RespondAsync($"`{timezone}` is not a valid IANA Time Zone ID (e.g. `Europe/Bucharest`, `America/New_York`). Please check the ID and try again.", ephemeral: true);
                return;
            }
            catch (InvalidTimeZoneException)
            {
                await RespondAsync($"`{timezone}` is an invalid timezone on this system.", ephemeral: true);
                return;
            }

            await _friendTimeSvc.AddOrUpdateFriendTimeAsync(Context.Guild.Id, EffectiveChannelId, targetUser.Id, tz.Id);

            var userDisplay = isSelf ? "Your" : $"{targetUser.Mention}'s";
            await RespondAsync($"{userDisplay} timezone for this channel has been set to `{tz.Id}`.");
        }

        [SlashCommand("remove", "Remove a user's timezone configuration (only admins can remove for other users)")]
        [CommandContextType(InteractionContextType.Guild)]
        public async Task FriendTimeRemove(
            [Summary(description: "The user to remove (admin only)")] SocketGuildUser user = null)
        {
            if (Context.Guild == null)
            {
                await RespondAsync("This command can only be used in a server channel.", ephemeral: true);
                return;
            }

            var targetUser = user ?? (SocketGuildUser)Context.User;
            var isSelf = targetUser.Id == Context.User.Id;
            var isAdmin = Context.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator;

            if (!isSelf && !isAdmin)
            {
                await RespondAsync("You do not have permission to remove another user's timezone. Only server administrators can do this.", ephemeral: true);
                return;
            }

            var removed = await _friendTimeSvc.RemoveFriendTimeAsync(Context.Guild.Id, EffectiveChannelId, targetUser.Id);

            if (removed)
            {
                var userDisplay = isSelf ? "Your" : $"{targetUser.Mention}'s";
                await RespondAsync($"{userDisplay} timezone configuration has been removed from this channel.");
            }
            else
            {
                var userDisplay = isSelf ? "You do not have" : $"{targetUser.Mention} does not have";
                await RespondAsync($"{userDisplay} a timezone configuration set up in this channel.", ephemeral: true);
            }
        }
    }
}
