using Discord;
using Discord.Commands;
using System.Linq;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    [RequireOwner]
    public class ApplyRoleToAllUsersModule : ModuleBase
    {
        [Command("applyroletoall")]
        public async Task ApplyRoleToAllUsers(IRole role)
        {
            var allUsers = (await Context.Guild.GetUsersAsync()).ToList();

            var msg = await ReplyAsync("Applying role to all users, please wait...");

            // this will most likely be rate limited to hell but I think the library handles that. we'll see.
            for (var i = 0; i < allUsers.Count; i++)
            {
                var user = allUsers[i];
                await user.AddRoleAsync(role);
                await msg.ModifyAsync(x => x.Content = $"Applying role to all users, please wait... {i + 1} of {allUsers.Count} completed");
            }

            await msg.ModifyAsync(x => x.Content = "Role successfully applied to all users");
        }
    }
}
