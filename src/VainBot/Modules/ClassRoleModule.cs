using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class ClassRoleModule : ModuleBase
    {
        private const ulong DRUID_ROLE_ID = 615650783018614803;
        private const ulong HUNTER_ROLE_ID = 615650691981115442;
        private const ulong MAGE_ROLE_ID = 615650575794831390;
        private const ulong PALADIN_ROLE_ID = 615650520513773568;
        private const ulong PRIEST_ROLE_ID = 615650890891657277;
        private const ulong ROGUE_ROLE_ID = 615650422018801747;
        private const ulong WARLOCK_ROLE_ID = 615650843672444952;
        private const ulong WARRIOR_ROLE_ID = 615650628588273689;

        [Command("druid")]
        public async Task AssignDruid()
        {
            await AssignRoleAsync(Context.User, Context.Guild, DRUID_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Druid role assigned.");
        }

        [Command("hunter")]
        public async Task AssignHunter()
        {
            await AssignRoleAsync(Context.User, Context.Guild, HUNTER_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Hunter role assigned.");
        }

        [Command("mage")]
        public async Task AssignMage()
        {
            await AssignRoleAsync(Context.User, Context.Guild, MAGE_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Mage role assigned.");
        }

        [Command("paladin")]
        public async Task AssignPaladin()
        {
            await AssignRoleAsync(Context.User, Context.Guild, PALADIN_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Paladin role assigned.");
        }

        [Command("priest")]
        public async Task AssignPriest()
        {
            await AssignRoleAsync(Context.User, Context.Guild, PRIEST_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Priest role assigned.");
        }

        [Command("rogue")]
        public async Task AssignRogue()
        {
            await AssignRoleAsync(Context.User, Context.Guild, ROGUE_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Rogue role assigned.");
        }

        [Command("warlock")]
        public async Task AssignWarlock()
        {
            await AssignRoleAsync(Context.User, Context.Guild, WARLOCK_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Warlock role assigned.");
        }

        [Command("warrior")]
        public async Task AssignWarrior()
        {
            await AssignRoleAsync(Context.User, Context.Guild, WARRIOR_ROLE_ID);
            await ReplyAsync($"{Context.User.Mention}: Warrior role assigned.");
        }

        private async Task AssignRoleAsync(IUser user, IGuild guild, ulong roleId)
        {
            await CastAndRemoveAllRolesAsync(user, guild);
            await CastAndAddRoleAsync(user, guild, roleId);
        }

        private async Task CastAndAddRoleAsync(IUser user, IGuild guild, ulong roleId)
        {
            var socketGuild = (SocketGuild)guild;
            var role = socketGuild.GetRole(roleId);

            await ((SocketGuildUser)user).AddRoleAsync(role);
        }

        private async Task CastAndRemoveAllRolesAsync(IUser user, IGuild guild)
        {
            await RemoveAllRolesAsync((SocketGuildUser)user, (SocketGuild)guild);
        }

        private async Task RemoveAllRolesAsync(SocketGuildUser user, SocketGuild guild)
        {
            var allRoles = new List<IRole>
            {
                guild.GetRole(DRUID_ROLE_ID),
                guild.GetRole(HUNTER_ROLE_ID),
                guild.GetRole(MAGE_ROLE_ID),
                guild.GetRole(PALADIN_ROLE_ID),
                guild.GetRole(PRIEST_ROLE_ID),
                guild.GetRole(ROGUE_ROLE_ID),
                guild.GetRole(WARLOCK_ROLE_ID),
                guild.GetRole(WARRIOR_ROLE_ID)
            };

            await user.RemoveRolesAsync(allRoles);
        }
    }
}
