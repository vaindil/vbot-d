using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace VainBot
{
    public static class Extensions
    {
        // https://stackoverflow.com/a/20175
        public static string ToOrdinal(this long num)
        {
            if (num <= 0)
                return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
            }
        }

        public static string ToOrdinal(this int num)
        {
            return ToOrdinal((long)num);
        }

        // tweaked from Discord.Addons.Interactive
        // https://github.com/foxbot/Discord.Addons.Interactive/blob/8eccd40d05c054b2304d9e5ef6e2fe340df528be/Discord.Addons.Interactive/InteractiveService.cs#L83
        public static async Task<IUserMessage> ReplyAndDeleteAsync(
            this SocketCommandContext context,
            string content, bool isTTS = false,
            Embed embed = null,
            TimeSpan? timeout = null,
            RequestOptions options = null)
        {
            timeout ??= TimeSpan.FromSeconds(5);

            var message = await context.Channel.SendMessageAsync(content, isTTS, embed, options).ConfigureAwait(false);
            _ = Task.Delay(timeout.Value)
                .ContinueWith(_ => message.DeleteAsync().ConfigureAwait(false))
                .ConfigureAwait(false);

            return message;
        }
    }
}
