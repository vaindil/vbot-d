using Discord;
using Discord.Commands;
using System;
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
        [CrendorMod]
        public async Task Toggle(IGuildUser user)
        {
            await ManageRole(user);
        }

        [Command]
        [CrendorMod]
        public async Task PingRole([Remainder]string message)
        {
            var role = Context.Guild.GetRole(418148201099689987);
            if (!role.IsMentionable)
                await role.ModifyAsync(r => r.Mentionable = true);

            await ReplyAsync($"{role.Mention}: {message}");

            await role.ModifyAsync(r => r.Mentionable = false);
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
