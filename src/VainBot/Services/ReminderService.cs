using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VainBot.Classes.Reminders;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class ReminderService
    {
        public const string SNOOZE_REMINDER_ID = "snoozeReminder";

        private readonly DiscordSocketClient _discord;
        private readonly DiscordRestClient _discordRest;

        private readonly ILogger<ReminderService> _logger;
        private readonly IServiceProvider _provider;
        private readonly IConfiguration _config;

        private readonly List<TimerWrapper> _timers = new();

        public ReminderService(
            DiscordSocketClient discord,
            DiscordRestClient discordRest,
            ILogger<ReminderService> logger,
            IServiceProvider provider,
            IConfiguration config)
        {
            _discord = discord;
            _discordRest = discordRest;

            _logger = logger;
            _provider = provider;
            _config = config;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing reminder service");
            List<Reminder> reminders;

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();
                var reminderQueryable = db.Reminders.AsQueryable().Where(x => x.IsActive);

                if (Program.IsDebug())
                {
                    var testGuildId = _config.GetValue<long>("test_guild_id");
                    reminders = await reminderQueryable.Where(x => x.GuildId == testGuildId).ToListAsync();

                    _logger.LogInformation("debug mode, test guild reminders retrieved from DB");
                }
                else
                {
                    reminders = await reminderQueryable.ToListAsync();

                    _logger.LogInformation("release mode, all reminders retrieved from DB");
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Reminder service could not be initialized");
                return;
            }

            var now = DateTimeOffset.UtcNow;

            for (var i = _timers.Count - 1; i >= 0; i--)
            {
                var t = _timers[i];
                t.Timer.Dispose();
                _timers.RemoveAt(i);
            }

            foreach (var r in reminders)
            {
                if (r.FireAt <= now)
                    await SendReminderAsync(r);
                else
                    CreateTimer(r);
            }
        }

        public async Task<Reminder> CreateReminderAsync(
            ulong userId, ulong channelId, ulong? messageId, ulong? guildId, string message, TimeSpan remindIn)
        {
            message = message.Replace("@everyone", "(@)everyone").Replace("@here", "(@)here");

            var reminder = new Reminder
            {
                CreatedAt = DateTimeOffset.UtcNow,
                FireAt = DateTimeOffset.UtcNow.Add(remindIn),
                UserId = (long)userId,
                ChannelId = (long)channelId,
                RequestingMessageId = messageId.HasValue ? (long)messageId.Value : -1,
                GuildId = (long?)guildId,
                Message = message
            };

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();
                db.Reminders.Add(reminder);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in reminder service: add reminder");
            }

            CreateTimer(reminder);

            return reminder;
        }

        public async Task UpdateReminderMessageIdAsync(Reminder reminder, ulong newMessageId)
        {
            try
            {
                reminder.RequestingMessageId = (long)newMessageId;
                using var db = _provider.GetRequiredService<VbContext>();
                db.Reminders.Update(reminder);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in reminder service: update reminder message ID");
            }

            CreateTimer(reminder);
        }

        public async Task SendReminderAsync(object reminderIn)
        {
            var reminder = (Reminder)reminderIn;

            var user = (IUser)_discord.GetUser((ulong)reminder.UserId) ?? await _discordRest.GetUserAsync((ulong)reminder.UserId);
            if (user == null)
            {
                await FinalizeReminderAsync(reminder);
                return;
            }

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(user)
                .WithFooter("Requested at " + reminder.CreatedAt.ToString("HH:mm yyyy-MM-dd") + " UTC")
                .WithColor(252, 185, 3)
                .AddField("Reminder", reminder.Message);

            MessageReference messageReference = null;

            // this may be set to -1 if an error occurred when updating the slash command message ID
            if (reminder.RequestingMessageId > 0)
            {
                messageReference = new MessageReference(messageId: (ulong)reminder.RequestingMessageId, failIfNotExists: false);
                // var guildId = reminder.GuildId.HasValue ? reminder.GuildId.ToString() : "@me";
                // embedBuilder.AddField(
                //     "Original Message",
                //     $"[Jump to message](https://discordapp.com/channels/{guildId}/{reminder.ChannelId}/{reminder.RequestingMessageId})");
            }

            var embed = embedBuilder.Build();

            if (!reminder.GuildId.HasValue)
            {
                var channel = await user.CreateDMChannelAsync();

                try
                {
                    await channel.SendMessageAsync(
                        user.Mention,
                        embed: embed,
                        messageReference: messageReference,
                        components: BuildSnoozeMenu(reminder.Id));
                }
                catch
                {
                    await FinalizeReminderAsync(reminder);
                    return;
                }
            }
            else
            {
                if (_discord.GetChannel((ulong)reminder.ChannelId) is not SocketTextChannel channel)
                {
                    _logger.LogInformation($"Could not send reminder to user {reminder.UserId} in channel {reminder.ChannelId}, " +
                        $"guild {reminder.GuildId}. Channel does not exist. Message: {reminder.Message}");

                    await FinalizeReminderAsync(reminder);
                    return;
                }

                try
                {
                    await channel.SendMessageAsync(
                        user.Mention,
                        embed: embed,
                        messageReference: messageReference,
                        components: BuildSnoozeMenu(reminder.Id));
                }
                catch
                {
                    _logger.LogWarning($"No permission to send reminder message to channel {channel.Name}. " +
                        $"Guild: {channel.Guild.Name}");
                }
            }

            await FinalizeReminderAsync(reminder);
        }

        public async Task<DateTimeOffset?> SnoozeReminderByIdAsync(int reminderId, TimeSpan snoozeFor)
        {
            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                var reminder = await db.Reminders.FindAsync(reminderId);
                if (reminder == null)
                    return null;

                reminder.IsActive = true;
                reminder.FireAt = DateTimeOffset.UtcNow.Add(snoozeFor);
                db.Reminders.Update(reminder);
                await db.SaveChangesAsync();

                CreateTimer(reminder);
                return reminder.FireAt;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in reminder service: snooze reminder");
            }

            return null;
        }

        public async Task<Reminder> GetReminderByIdAsync(int reminderId)
        {
            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                return await db.Reminders.FindAsync(reminderId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error getting reminder in reminder service");
                return null;
            }
        }

        private static MessageComponent BuildSnoozeMenu(int reminderId)
        {
            var menuBuilder = new SelectMenuBuilder()
                .WithCustomId($"{SNOOZE_REMINDER_ID}:{reminderId}")
                .WithMinValues(1)
                .WithMaxValues(1)
                .WithPlaceholder("Snooze reminder?")
#if DEBUG
                .AddOption("Immediate (testing only)", "1")
#endif
                .AddOption("10 minutes", "10")
                .AddOption("30 minutes", "30")
                .AddOption("1 hour", "60")
                .AddOption("2 hours", "120")
                .AddOption("4 hours", "240")
                .AddOption("8 hours", "480")
                .AddOption("1 day", "1440")
                .AddOption("2 days", "2880")
                .AddOption("1 week", "10080");

            return new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .Build();
        }

        private async Task FinalizeReminderAsync(Reminder reminder)
        {
            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                var thisReminder = await db.Reminders.FindAsync(reminder.Id);
                if (thisReminder != null)
                {
                    thisReminder.IsActive = false;
                    db.Reminders.Update(thisReminder);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in reminder service: remove reminder");
                return;
            }

            var wrapper = _timers.Find(t => t.ReminderId == reminder.Id);
            if (wrapper == null)
                return;

            wrapper.Timer.Dispose();
            wrapper.Timer = null;

            _timers.Remove(wrapper);
        }

        private void CreateTimer(object reminderIn)
        {
            var reminder = (Reminder)reminderIn;

            var wrapper = _timers.Find(t => t.ReminderId == reminder.Id);
            if (wrapper != null)
            {
                wrapper.Timer.Dispose();
                wrapper.Timer = null;

                _timers.Remove(wrapper);
            }

            var timeSpan = reminder.FireAt - DateTimeOffset.UtcNow;
            var maxTimeSpan = TimeSpan.FromDays(40);

            if (timeSpan > maxTimeSpan)
            {
                _timers.Add(
                    new TimerWrapper(
                        reminder.Id,
                        reminder.UserId,
                        new Timer(CreateTimer, reminder, maxTimeSpan, TimeSpan.FromMilliseconds(-1))));
            }
            else
            {
                _timers.Add(
                    new TimerWrapper(
                        reminder.Id,
                        reminder.UserId,
                        new Timer(async (e) => await SendReminderAsync(e), reminder, timeSpan, TimeSpan.FromMilliseconds(-1))));
            }
        }

        private class TimerWrapper
        {
            public TimerWrapper(int reminderId, long userId, Timer timer)
            {
                ReminderId = reminderId;
                UserId = userId;
                Timer = timer;
            }

            public int ReminderId { get; set; }

            public long UserId { get; set; }

            public Timer Timer { get; set; }
        }
    }
}
