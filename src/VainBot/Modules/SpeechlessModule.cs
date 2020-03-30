using Discord.Commands;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class SpeechlessModule : ModuleBase
    {
        [Command("speechless")]
        [Alias("sl", "what", "what?")]
        public async Task Speechless([Remainder]string unused = null)
        {
            await ReplyAsync("https://vaindil.com/sl.gif");
        }
    }
}
