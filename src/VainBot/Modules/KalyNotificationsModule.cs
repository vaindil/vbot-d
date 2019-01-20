using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    [Group("notifications")]
    [Alias("notification")]
    public class KalyNotificationsModule : ModuleBase
    {
        private const ulong _roleId = 534563536148627466;

        [Command]
        public async Task ToggleNotifications([Remainder]string unused = null)
        {
            var user = Context.User as SocketGuildUser;
            if (user.Roles.Any(x => x.Id == _roleId))
                await NotificationsOff();
            else
                await NotificationsOn();
        }

        [Command("on", RunMode = RunMode.Async)]
        [Alias("yes")]
        public async Task NotificationsOn([Remainder]string unused = null)
        {
            var role = Context.Guild.GetRole(_roleId);
            var user = Context.User as SocketGuildUser;

            await user.AddRoleAsync(role);

            var msg = await ReplyAsync($"{Context.User.Mention}: You will now receive notifications for Kaly's streams.");
        }

        [Command("off", RunMode = RunMode.Async)]
        [Alias("no")]
        private async Task NotificationsOff([Remainder]string unused = null)
        {
            var role = Context.Guild.GetRole(_roleId);
            var user = Context.User as SocketGuildUser;

            await user.RemoveRoleAsync(role);

            var msg = await ReplyAsync($"{Context.User.Mention}: You will no longer receive notifications for Kaly's streams.");
        }
    }
}
