using Discord.Commands;
using Discord.WebSocket;
using System.Linq;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class KalyArtistModule : ModuleBase
    {
        [Command("artist")]
        public async Task ToggleArtistRole()
        {
            const ulong roleId = 510288563766820875;

            // checked here and not in a precondition; a precondition would throw an error if invalid.
            // this is used on multiple servers, so just fail silently if it's the wrong guild.
            if (Context.Guild.Id != 258507766669377536)
                return;

            // right guild but wrong channel
            if (Context.Channel.Id != 298307502590918656 && Context.Channel.Id != 433429460822523934)
                return;

            var role = Context.Guild.GetRole(roleId);

            var user = (SocketGuildUser)Context.User;
            if (!user.Roles.Any(x => x.Id == roleId))
            {
                await user.AddRoleAsync(role);
                await ReplyAsync($"{Context.User.Mention}: Artist role added.");
            }
            else
            {
                await user.RemoveRoleAsync(role);
                await ReplyAsync($"{Context.User.Mention}: Artist role removed.");
            }
        }
    }
}
