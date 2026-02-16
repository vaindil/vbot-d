using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YahooFinanceApi;

namespace VainBot.SlashCommandModules
{
    [Group("stock", "Stock market info")]
    public class StockSlashCommandGroupModule(ILogger<StockSlashCommandGroupModule> _logger) : InteractionModuleBase<SocketInteractionContext>
    {
        // overview of the markets for today
        // /stock today
        //[SlashCommand("overview", "Overall market info")]
        //[CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        //public async Task StockMarketOverview()
        //{
        //    var markets = new[] { "^DJI" };
        //    var quotes = await Yahoo.Symbols(markets)
        //        .Fields(Field.Symbol, Field.Market, Field.RegularMarketOpen, Field.RegularMarketPreviousClose, Field.MarketState)
        //        .QueryAsync();

        //    // build embed
        //    await RespondAsync("Queried", ephemeral: true);
        //}

        // specific stock info
        // /stock price <symbol>
        [SlashCommand("price", "Price info for a specific stock")]
        [CommandContextType(InteractionContextType.Guild, InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
        public async Task StockMarketPrice([MinLength(1), Summary(description: "Symbol to look up")]string symbol)
        {
            var fullResponse = await Yahoo.Symbols([symbol])
                .Fields(
                    Field.Symbol,
                    Field.LongName,
                    Field.MarketState,
                    Field.Currency,
                    Field.Market,
                    Field.RegularMarketOpen,
                    Field.RegularMarketChange,
                    Field.RegularMarketChangePercent,
                    Field.RegularMarketPreviousClose,
                    Field.PostMarketPrice,
                    Field.PostMarketChange,
                    Field.PostMarketChangePercent)
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
                embed.WithFooter($"{record.LongName} | {(record.MarketState == "OPEN" ? GreenCircle() : RedCircle())} Market is {record.MarketState.ToLower()}");

            // if market is open: open, current
            if (record.MarketState == "OPEN")
            {
                if (ContainsRegularMarketKeys(record.Fields))
                    embed.AddField("Current price", $"{record.RegularMarketOpen} ({Math.Round(record.RegularMarketChange, 2)}, {Math.Round(record.RegularMarketChangePercent, 2)})");
            }
            // if market is closed: last close (w/ % change) and current after-hours prices (w/ % change)
            else
            {
                if (ContainsCloseKeys(record.Fields))
                    embed.AddField("Last Close", $"{record.RegularMarketPreviousClose} ({Math.Round(record.RegularMarketChange, 2)}, {Math.Round(record.RegularMarketChangePercent, 2)}%)", inline: true);

                if (ContainsPostMarketKeys(record.Fields))
                    embed.AddField("After Hours", $"{record.PostMarketPrice} ({Math.Round(record.PostMarketChange, 2)}, {Math.Round(record.PostMarketChangePercent, 2)}%)", inline: true);
            }

            if (embed.Fields.Count == 0)
            {
                await RespondAsync($"`{symbol}` doesn't seem to be a valid symbol. Try [looking it up on Yahoo Finance](<https://finance.yahoo.com/lookup/?s={symbol}>).", ephemeral: true);
                return;
            }

            await RespondAsync(embeds: [embed.Build()]);
        }

        private static string GreenCircle()
        {
            return new Emoji("\uD83D\uDFE2").ToString();
        }

        private static string RedCircle()
        {
            return new Emoji("\uD83D\uDD34").ToString();
        }

        private static bool ContainsRegularMarketKeys(IReadOnlyDictionary<string, dynamic> fields)
        {
            return
                fields.ContainsKey("RegularMarketOpen") &&
                fields.ContainsKey("RegularMarketChange") &&
                fields.ContainsKey("RegularMarketChangePercent");
        }

        private static bool ContainsCloseKeys(IReadOnlyDictionary<string, dynamic> fields)
        {
            return
                fields.ContainsKey("RegularMarketPreviousClose") &&
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
