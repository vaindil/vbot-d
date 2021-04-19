using Discord.Commands;
using System.Threading.Tasks;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [ZubatGuild]
    public class CarlitosModule : ModuleBase
    {
        [Command("carlitos")]
        [Alias("carlos", "david", "globes")]
        public async Task Carlitos([Remainder]string _ = null)
        {
            await ReplyAsync("<:Bedge:833494776137121834> <:thought1:833494788174249985> Carlitos getting owned by globes <:thought2:833494796156272652>");
        }
    }
}
