using Discord.Commands;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    public class TemperatureModule : ModuleBase
    {
        private static readonly char[] validUnits = ['f', 'c', 'k'];

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
            var lastChar = input[^1];

            if (!validUnits.Contains(lastChar))
            {
                await ReplyAsync(reply);
                return;
            }

            if (!decimal.TryParse(input.AsSpan(0, input.Length - 1), out var val))
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
                reply = $"{val} °F is {c} °C";
            }
            else if (lastChar == 'c')
            {
                var f = Math.Round(((val * 9) / 5) + 32, 2);
                reply = $"{val} °C is {f} °F";
            }
            else
            {
                var c = val - (decimal)273.15;
                var f = Math.Round(((c * 9) / 5) + 32, 2);
                reply = $"{val} K is {c} °C, {f} °F";
            }

            await ReplyAsync(reply);
        }
    }
}
