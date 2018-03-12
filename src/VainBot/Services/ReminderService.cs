using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VainBot.Classes;

namespace VainBot.Services
{
    public class ReminderService
    {
        readonly DiscordSocketClient _discord;
        readonly IServiceProvider _provider;

        readonly List<TimerWrapper> _timers = new List<TimerWrapper>();

        public ReminderService(
            DiscordSocketClient discord,
            IServiceProvider provider)
        {
            _discord = discord;
            _provider = provider;
        }

        public async Task InitializeAsync()
        {
            List<Reminder> reminders;
            using (var db = _provider.GetRequiredService<VbContext>())
            {
                reminders = await db.Reminders.ToListAsync();
            }

            var guilds = reminders.Select(r => _discord.GetGuild((ulong)r.GuildId)).Distinct();
            await _discord.DownloadUsersAsync(guilds);

            var now = DateTimeOffset.UtcNow;

            foreach (var r in reminders)
            {
                if (r.FireAt <= now)
                {
                    await SendReminderAsync(r);
                }
                else
                {
                    var timeSpan = r.FireAt - now;
                    _timers.Add(
                        new TimerWrapper(
                            r.Id,
                            new Timer(async (e) => await SendReminderAsync(e), r, timeSpan, TimeSpan.FromMilliseconds(-1))));
                }
            }
        }

        public async Task CreateReminderAsync(ulong userId, ulong channelId, ulong guildId, bool isDM, string message, TimeSpan remindIn)
        {
            message = message.Replace("@everyone", "(@)everyone").Replace("@here", "(@)here");

            var reminder = new Reminder
            {
                CreatedAt = DateTimeOffset.UtcNow,
                FireAt = DateTimeOffset.UtcNow.Add(remindIn),
                UserId = (long)userId,
                ChannelId = (long)channelId,
                GuildId = (long)guildId,
                IsDM = isDM,
                Message = message
            };

            using (var db = _provider.GetRequiredService<VbContext>())
            {
                db.Reminders.Add(reminder);
                await db.SaveChangesAsync();
            }

            _timers.Add(
                new TimerWrapper(
                    reminder.Id,
                    new Timer(async (e) => await SendReminderAsync(e), reminder, remindIn, TimeSpan.FromMilliseconds(-1))));
        }

        public async Task SendReminderAsync(object reminderIn)
        {
            var reminder = (Reminder)reminderIn;

            var user = _discord.GetUser((ulong)reminder.UserId);
            if (user == null)
                return;

            var message = $"{user.Mention} asked for a reminder: {reminder.Message}";

            if (reminder.IsDM)
            {
                var channel = await user.GetOrCreateDMChannelAsync();
                if (channel == null)
                    return;

                await channel.SendMessageAsync(message);
            }
            else
            {
                var channel = _discord.GetChannel((ulong)reminder.ChannelId) as SocketTextChannel;
                if (channel == null)
                    return;

                await channel.SendMessageAsync(message);
            }

            using (var db = _provider.GetRequiredService<VbContext>())
            {
                var thisReminder = await db.Reminders.FindAsync(reminder.Id);
                if (thisReminder != null)
                {
                    db.Reminders.Remove(thisReminder);
                    await db.SaveChangesAsync();
                }
            }

            var wrapper = _timers.Find(t => t.ReminderId == reminder.Id);
            if (wrapper == null)
                return;

            wrapper.Timer.Dispose();
            wrapper.Timer = null;

            _timers.Remove(wrapper);
        }

        private class TimerWrapper
        {
            public TimerWrapper(int reminderId, Timer timer)
            {
                ReminderId = reminderId;
                Timer = timer;
            }

            public int ReminderId { get; set; }

            public Timer Timer { get; set; }
        }
    }
}
