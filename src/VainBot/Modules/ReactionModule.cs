using Discord.Commands;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class ReactionModule : ModuleBase
    {
        const string _baseUrl = "https://vaindil.com/r/";

        [Command("butwhy")]
        [Alias("bw", "why")]
        public async Task ButWhy([Remainder] string _ = null)
        {
            await ReplyWithReactionAsync("bw.gif");
        }

        [Command("speechless")]
        [Alias("sl", "what", "what?")]
        public async Task Speechless([Remainder] string _ = null)
        {
            await ReplyWithReactionAsync("sl.gif");
        }

        [Command("wink")]
        [Alias("agathaWink", "agatha")]
        public async Task Wink([Remainder] string _ = null)
        {
            await ReplyWithReactionAsync("agathaWink.gif");
        }

        [Command("catLick")]
        [Alias("lick")]
        public async Task CatLick([Remainder] string _ = null)
        {
            await ReplyWithReactionAsync("catLick.gif");
        }

        private async Task ReplyWithReactionAsync(string filename)
        {
            await ReplyAsync(_baseUrl + filename);
        }
    }
}
