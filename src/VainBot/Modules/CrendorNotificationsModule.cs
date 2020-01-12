using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [Group("notifications")]
    [Alias("notification")]
    [CrendorGuild]
    public class CrendorNotificationsModule : ModuleBase
    {
        private const ulong _roleId = 665991203845570614;

        [Command]
        public async Task ToggleNotifications()
        {
            var user = Context.User as SocketGuildUser;
            if (user.Roles.Any(x => x.Id == _roleId))
                await NotificationsOff();
            else
                await NotificationsOn();
        }

        [Command("on", RunMode = RunMode.Async)]
        [Alias("yes")]
        public async Task NotificationsOn()
        {
            var role = Context.Guild.GetRole(_roleId);
            var user = Context.User as SocketGuildUser;

            await user.AddRoleAsync(role);

            var msg = await ReplyAsync($"{Context.User.Mention}: You will now receive notifications for Crendor's streams.");

            await Task.Delay(4000);

            await Context.Message.DeleteAsync();
            await msg.DeleteAsync();
        }

        [Command("off", RunMode = RunMode.Async)]
        [Alias("no")]
        private async Task NotificationsOff()
        {
            var role = Context.Guild.GetRole(_roleId);
            var user = Context.User as SocketGuildUser;

            await user.RemoveRoleAsync(role);

            var msg = await ReplyAsync($"{Context.User.Mention}: You will no longer receive notifications for Crendor's streams.");

            await Task.Delay(4000);

            await Context.Message.DeleteAsync();
            await msg.DeleteAsync();
        }
    }
}
