using Discord.Commands;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class MiscModule : ModuleBase
    {
        [Command("wordle")]
        public async Task Wordle([Remainder]string _ = null)
        {
            await ReplyAsync("<https://www.powerlanguage.co.uk/wordle/>");
        }
    }
}
