using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace VainBot.Services
{
    public class ActionChannelGuard
    {
        private readonly DiscordSocketClient _discord;
        private readonly bool _isDev;

        public ActionChannelGuard(DiscordSocketClient discord)
        {
            _discord = discord;
            _isDev = Environment.GetEnvironmentVariable("VB_DEV") != null;

            _discord.MessageReceived += ChannelGuard;
        }

        private async Task ChannelGuard(SocketMessage message)
        {
            var prefix = _isDev ? '+' : '!';
            var valid = prefix + "reason";

            if (message.Channel.Id == 480178651837628436 && !message.Author.IsBot && !message.Content.StartsWith(valid))
            {
                await message.DeleteAsync();

                var botMsg = await message.Channel.SendMessageAsync($"{message.Author.Mention}: No chatting in this channel.");

                await Task.Delay(5000);
                await botMsg.DeleteAsync();
            }
        }
    }
}
