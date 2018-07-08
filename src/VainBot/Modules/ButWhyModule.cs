using Discord.Commands;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class ButWhyModule : ModuleBase
    {
        [Command("butwhy")]
        [Alias("bw", "why")]
        public async Task ButWhy([Remainder]string unused = null)
        {
            await ReplyAsync("https://vaindil.com/bw.gif");
        }
    }
}
