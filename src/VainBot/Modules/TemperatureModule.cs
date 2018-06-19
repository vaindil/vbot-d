using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class TemperatureModule : ModuleBase
    {
        [Command("temp")]
        [Alias("temperature")]
        public async Task Temperature([Remainder]string input)
        {
            var reply = "Example: `!temp 45c` or `!temp 12f`";
            if (string.IsNullOrWhiteSpace(input))
            {
                await ReplyAsync(reply);
                return;
            }

            input = input.Trim().ToLowerInvariant();
            var lastChar = input[input.Length - 1];

            if (lastChar != 'f' && lastChar != 'c')
            {
                await ReplyAsync(reply);
                return;
            }

            if (!decimal.TryParse(input.Substring(0, input.Length - 1), out var val))
            {
                await ReplyAsync(reply);
                return;
            }

            val = Math.Round(val, 2);

            if (Math.Abs(val) > 100000)
            {
                await ReplyAsync("That's a bit much, calm down.");
                return;
            }

            if (lastChar == 'f')
            {
                var c = Math.Round(((val - 32) * 5) / 9, 2);
                reply = $"{val}° F is {c}° C";
            }
            else
            {
                var f = Math.Round(((val * 9) / 5) + 32, 2);
                reply = $"{val}° C is {f}° F";
            }

            await ReplyAsync(reply);
        }
    }
}
