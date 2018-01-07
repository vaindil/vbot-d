using Discord.WebSocket;
using Hangfire;
using System;
using System.Threading.Tasks;

namespace VainBotDiscord.Services
{
    public class ReminderService
    {
        readonly DiscordSocketClient _discord;

        public ReminderService(DiscordSocketClient discord)
        {
            _discord = discord;
        }

        public void CreateReminder(ulong userId, ulong channelId, bool isDM, string message, TimeSpan remindIn)
        {
            var wrapper = new ReminderWrapper(userId, channelId, isDM, message);
            BackgroundJob.Schedule(() => SendReminderAsync(wrapper), remindIn);
        }

        public async Task SendReminderAsync(ReminderWrapper wrapper)
        {
            var user = _discord.GetUser(wrapper.UserId);
            if (user == null)
                return;

            var message = $"{user.Mention} asked for a reminder: {wrapper.Message}";

            if (wrapper.IsDM)
            {
                var channel = await user.GetOrCreateDMChannelAsync();
                if (channel == null)
                    return;

                await channel.SendMessageAsync(message);
            }
            else
            {
                var channel = _discord.GetChannel(wrapper.ChannelId) as SocketTextChannel;
                if (channel == null)
                    return;

                await channel.SendMessageAsync(message);
            }
        }

        public class ReminderWrapper
        {
            public ReminderWrapper(ulong userId, ulong channelId, bool isDM, string message)
            {
                UserId = userId;
                ChannelId = channelId;
                IsDM = isDM;
                Message = message;
            }

            public ulong UserId { get; set; }

            public ulong ChannelId { get; set; }

            public bool IsDM { get; set; }

            public string Message { get; set; }
        }
    }
}
