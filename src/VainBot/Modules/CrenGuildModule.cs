using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [Group("crenguild")]
    [Alias("guild")]
    [CrendorGuild]
    public class CrenGuildModule : ModuleBase
    {
        [Command]
        public async Task ToggleSelf()
        {
            await ManageRole((IGuildUser)Context.User);
        }

        [Command]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Toggle(IGuildUser user)
        {
            await ManageRole(user);
        }

        private async Task ManageRole(IGuildUser user)
        {
            var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == "CrenGuild");
            if (role == null)
                return;

            if (user.RoleIds.Contains(role.Id))
            {
                await user.RemoveRoleAsync(role);
                await ReplyAsync($"CrenGuild role removed from {user.Mention}.");
            }
            else
            {
                await user.AddRoleAsync(role);
                await ReplyAsync($"CrenGuild role added to {user.Mention}.");
            }
        }
    }
}
