using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VainBot.SlashCommandModules
{
    [DontAutoRegister]
    [Group("blood-bowl", "Commands for handling Crendor Blood Bowl roles")]
    public class CrendorBloodBowlSlashCommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        private const ulong BLOOD_BOWL_LFG_ID = 780458904328208395;
        private const string BLOOD_BOWL_LFG_NAME = "Blood Bowl LFG";

        private const ulong CRENBOWL_NEWS_ID = 780478051627565086;
        private const string CRENBOWL_NEWS_NAME = "Crenbowl News";

        [SlashCommand("lfg", $"Toggle the \"{BLOOD_BOWL_LFG_NAME}\" role for yourself to be pinged when people are looking to play")]
        public async Task ToggleLfgRoleAsync()
        {
            await ToggleRoleAsync(BLOOD_BOWL_LFG_ID, BLOOD_BOWL_LFG_NAME);
        }

        [SlashCommand(
            "news",
            $"Toggle the \"{CRENBOWL_NEWS_NAME}\" role for yourself to be pinged with Crenbowl League news")]
        public async Task ToggleNewsRoleAsync()
        {
            await ToggleRoleAsync(CRENBOWL_NEWS_ID, CRENBOWL_NEWS_NAME);
        }

        private async Task ToggleRoleAsync(ulong roleId, string roleName)
        {
            var user = (SocketGuildUser)Context.User;
            if (user.Roles.Any(x => x.Id == roleId))
            {
                await user.RemoveRoleAsync(roleId);
                await RespondAsync($"You have been removed from the {roleName} role.", ephemeral: true);
            }
            else
            {
                await user.AddRoleAsync(roleId);
                await RespondAsync($"You have been added to the {roleName} role.", ephemeral: true);
            }
        }
    }
}
