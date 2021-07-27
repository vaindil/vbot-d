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

        [Command("whoasked")]
        [Alias("askers", "wa", "who")]
        public async Task WhoAsked([Remainder] string _ = null)
        {
            await ReplyWithReactionAsync("wa.mp4");
        }

        [Command("catLick")]
        [Alias("lick")]
        public async Task CatLick([Remainder] string _ = null)
        {
            await ReplyWithReactionAsync("catLick.gif");
        }

        [Command("catLickScuffed")]
        [Alias("lickScuffed", "scuffedLick")]
        public async Task CatLickScuffed([Remainder] string _ = null)
        {
            await ReplyWithReactionAsync("catLickScuffed.gif");
        }

        private async Task ReplyWithReactionAsync(string filename)
        {
            await ReplyAsync(_baseUrl + filename);
        }
    }
}
