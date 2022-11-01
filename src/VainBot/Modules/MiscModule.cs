using Discord.Commands;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class MiscModule : ModuleBase
    {
        [Command("wordle")]
        [Alias("contexto")]
        public async Task Wordle([Remainder]string _ = null)
        {
            await ReplyAsync("<https://www.nytimes.com/games/wordle/index.html> | <https://scoredle.com> | <https://contexto.me>");
        }
    }
}
