using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VainBotDiscord.Modules
{
    [Group("coins")]
    [Alias("coin", "btc", "eth", "ltc", "iot")]
    public class CoinModule : ModuleBase
    {
        readonly HttpClient _httpClient;

        public CoinModule(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [Command]
        public async Task GetCoins([Remainder]string unused = null)
        {
            // Bitfenix's API isn't exactly the greatest, so I want some indication that the bot is trying
            await Context.Channel.TriggerTypingAsync();

            var response = await _httpClient.GetAsync(
                "https://api.bitfinex.com/v2/tickers?symbols=tBTCUSD,tETHUSD,tLTCUSD,tIOTUSD,tXMRUSD");
            if (!response.IsSuccessStatusCode)
            {
                await ReplyAsync("Bitfenix's API crapped out. Not my fault, sorry. Try again in a few seconds.");
                return;
            }

            // Bitfenix's API is terrible, they don't use key/value pairs because that would make too much sense
            // https://bitfinex.readme.io/v2/reference#rest-public-tickers
            var results = JsonConvert.DeserializeObject<List<List<object>>>(await response.Content.ReadAsStringAsync());
            var coins = ConvertToCoins(results);

            var btc = coins.Find(c => c.Symbol == "tBTCUSD");
            var eth = coins.Find(c => c.Symbol == "tETHUSD");
            var ltc = coins.Find(c => c.Symbol == "tLTCUSD");
            var iot = coins.Find(c => c.Symbol == "tIOTUSD");
            var xmr = coins.Find(c => c.Symbol == "tXMRUSD");

            var message = new StringBuilder();
            message.Append("BTC: ");
            message.Append(btc.LastPrice.ToString("#.00#"));
            message.Append("\n");

            message.Append("ETH: ");
            message.Append(eth.LastPrice.ToString("#.00#"));
            message.Append("\n");

            message.Append("LTC: ");
            message.Append(ltc.LastPrice.ToString("#.00#"));
            message.Append("\n");

            message.Append("IOT: ");
            message.Append(iot.LastPrice.ToString("#.00#"));
            message.Append("\n");

            message.Append("XMR: ");
            message.Append(xmr.LastPrice.ToString("#.00#"));

            await ReplyAsync(message.ToString());
        }

        List<Coin> ConvertToCoins(List<List<object>> obj)
        {
            var coins = new List<Coin>();

            foreach (var coin in obj)
            {
                coins.Add(new Coin
                {
                    Symbol = (string)coin[0],
                    Bid = Convert.ToDecimal(coin[1]),
                    BidSize = Convert.ToDecimal(coin[2]),
                    Ask = Convert.ToDecimal(coin[3]),
                    AskSize = Convert.ToDecimal(coin[4]),
                    DailyChange = Convert.ToDecimal(coin[5]),
                    DailyChangePercentage = Convert.ToDecimal(coin[6]),
                    LastPrice = Convert.ToDecimal(coin[7]),
                    Volume = Convert.ToDecimal(coin[8]),
                    High = Convert.ToDecimal(coin[9]),
                    Low = Convert.ToDecimal(coin[10])
                });
            }

            return coins;
        }

        class Coin
        {
            public string Symbol { get; set; }
            public decimal Bid { get; set; }
            public decimal BidSize { get; set; }
            public decimal Ask { get; set; }
            public decimal AskSize { get; set; }
            public decimal DailyChange { get; set; }
            public decimal DailyChangePercentage { get; set; }
            public decimal LastPrice { get; set; }
            public decimal Volume { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
        }
    }
}
