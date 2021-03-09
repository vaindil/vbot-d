using Discord.Commands;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class ReactionModule : ModuleBase
    {
        [Command("butwhy")]
        [Alias("bw", "why")]
        public async Task ButWhy([Remainder] string _ = null)
        {
            await ReplyAsync("https://vaindil.com/bw.gif");
        }

        [Command("speechless")]
        [Alias("sl", "what", "what?")]
        public async Task Speechless([Remainder] string _ = null)
        {
            await ReplyAsync("https://vaindil.com/sl.gif");
        }

        [Command("wink")]
        [Alias("agathaWink", "agatha")]
        public async Task Wink([Remainder] string _ = null)
        {
            await ReplyAsync("https://vaindil.com/agathaWink.gif");
        }
    }
}
