using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace VainBot.SlashCommandModules
{
    [Group("stock", "Stock market info")]
    public class StockSlashCommandGroupModule() : InteractionModuleBase<SocketInteractionContext>
    {
        // overview of the markets
        // /stock index
        [SlashCommand("index", "Info for Dow, S&P 500, and NASDAQ")]
        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        public async Task StockMarketOverview()
        {
            var markets = new[] { "^DJI", "^GSPC", "^IXIC" };
            var fullResponse = await Yahoo.Symbols(markets)
                .Fields(Fields)
                .QueryAsync();

            if (fullResponse.Count != 3)
            {
                await RespondAsync("Error contacting Yahoo Finance, please try again.", ephemeral: true);
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Stock index info")
                .WithUrl("https://finance.yahoo.com/markets/")
                .WithFooter(MarketStateString(fullResponse.Values.First().MarketState));

            foreach (var records in fullResponse.Values)
                AddFieldsToEmbed(embed, records, true);

            await RespondAsync(embeds: [embed.Build()]);
        }

        // specific stock info
        // /stock price <symbol>
        [SlashCommand("price", "Price info for a specific stock")]
        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        public async Task StockMarketPrice([MinLength(1), Summary(description: "Symbol to look up")]string symbol)
        {
            var fullResponse = await Yahoo.Symbols([symbol])
                .Fields(Fields)
                .QueryAsync();

            if (fullResponse.Count != 1)
            {
                await RespondAsync($"`{symbol}` is not a valid symbol.", ephemeral: true);
                return;
            }

            var record = fullResponse[symbol.ToUpper()];
            var embed = new EmbedBuilder()
                .WithTitle($"Stock info for {record.Symbol}")
                .WithUrl($"https://finance.yahoo.com/quote/{symbol}/");

            if (record.Fields.ContainsKey("LongName") && record.Fields.ContainsKey("FullExchangeName"))
                embed.WithFooter($"{record.LongName} | {MarketStateString(record.MarketState)}");

            AddFieldsToEmbed(embed, record, false);

            if (embed.Fields.Count == 0)
            {
                await RespondAsync($"`{symbol}` doesn't seem to be a valid symbol. Try [looking it up on Yahoo Finance](<https://finance.yahoo.com/lookup/?s={symbol}>).", ephemeral: true);
                return;
            }

            await RespondAsync(embeds: [embed.Build()]);
        }

        private static void AddFieldsToEmbed(EmbedBuilder embed, Security record, bool multiSymbol)
        {
            var nameTitle = "";
            if (multiSymbol && !string.IsNullOrEmpty(record.ShortName))
                nameTitle = $" ({record.ShortName})";

            // if market is open: open, current
            if (record.MarketState == "REGULAR")
            {
                if (ContainsRegularMarketKeys(record.Fields))
                    embed.AddField($"Current price{nameTitle}", $"{record.RegularMarketPrice} ({Math.Round(record.RegularMarketChange, 2)}, {Math.Round(record.RegularMarketChangePercent, 2)}%)");
            }
            // if market is closed: last close (w/ % change) and current after-hours prices (w/ % change)
            else
            {
                if (ContainsCloseKeys(record.Fields))
                    embed.AddField($"Last Close{nameTitle}", $"{record.RegularMarketPrice} ({Math.Round(record.RegularMarketChange, 2)}, {Math.Round(record.RegularMarketChangePercent, 2)}%)", inline: !multiSymbol);

                // fields are not accurate and i can't find a replacement
                //if (ContainsPostMarketKeys(record.Fields))
                //    embed.AddField($"After Hours{nameTitle}", $"{record.PostMarketPrice} ({Math.Round(record.PostMarketChange, 2)}, {Math.Round(record.PostMarketChangePercent, 2)}%)", inline: !multiSymbol);
            }
        }

        private static readonly Field[] Fields = [
            Field.Symbol,
            Field.ShortName,
            Field.LongName,
            Field.MarketState,
            Field.Market,
            Field.RegularMarketOpen,
            Field.RegularMarketChange,
            Field.RegularMarketChangePercent,
            Field.PostMarketPrice,
            Field.PostMarketChange,
            Field.PostMarketChangePercent
        ];

        private static string MarketStateString(string marketState)
        {
            if (marketState == "REGULAR")
                return $"{new Emoji("\uD83D\uDFE2")} Market is open";
            else
                return $"{new Emoji("\uD83D\uDD34")} Market is closed";
        }

        private static bool ContainsRegularMarketKeys(IReadOnlyDictionary<string, dynamic> fields)
        {
            return
                fields.ContainsKey("RegularMarketPrice") &&
                fields.ContainsKey("RegularMarketChange") &&
                fields.ContainsKey("RegularMarketChangePercent");
        }

        private static bool ContainsCloseKeys(IReadOnlyDictionary<string, dynamic> fields)
        {
            return
                fields.ContainsKey("RegularMarketPrice") &&
                fields.ContainsKey("RegularMarketChange") &&
                fields.ContainsKey("RegularMarketChangePercent");
        }

        private static bool ContainsPostMarketKeys(IReadOnlyDictionary<string, dynamic> fields)
        {
            return
                fields.ContainsKey("PostMarketPrice") &&
                fields.ContainsKey("PostMarketChange") &&
                fields.ContainsKey("PostMarketChangePercent");
        }
    }
}
