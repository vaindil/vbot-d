using Discord.Commands;
using System;
using System.Text;
using System.Threading.Tasks;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [FitzyModerator]
    public class GintokiModule : ModuleBase
    {
        private readonly Random _random;

        public GintokiModule(Random random)
        {
            _random = random;
        }

        [Command("gintoki")]
        [Alias("gin")]
        public async Task Gintoki([Remainder]string message)
        {
            var outMsg = new StringBuilder();
            foreach (var l in message)
            {
                if (!char.IsLetter(l))
                {
                    outMsg.Append(l);
                    continue;
                }

                var final = l;

                if (_random.Next(0, 10) >= 6)
                {
                    if (char.IsUpper(l))
                        final = char.ToLower(l);
                    else
                        final = char.ToUpper(l);
                }

                outMsg.Append(final);
            }

            await ReplyAsync(outMsg.ToString());
        }
    }
}
