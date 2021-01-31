using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace VainBot.Modules
{
    [Group("coins")]
    [Alias("coin", "btc", "eth", "ltc", "iot", "doge", "dog", "xdg", "dge")]
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
            HttpResponseMessage bfResponse;

            await Context.Channel.TriggerTypingAsync();
            try
            {
                bfResponse = await _httpClient.GetAsync("https://api-pub.bitfinex.com/v2/tickers?symbols=tBTCUSD,tETHUSD,tLTCUSD,tIOTUSD,tXMRUSD,tDOGUSD");
            }
            catch
            {
                await ReplyAsync("Bitfinex's API crapped out. Not my fault, sorry. Try again in a few seconds.");
                return;
            }

            if (!bfResponse.IsSuccessStatusCode)
            {
                await ReplyAsync("Bitfinex's API crapped out. Not my fault, sorry. Try again in a few seconds.");
                return;
            }

            // Bitfinex's API doesn't use key/value pairs because that would make too much sense
            // https://docs.bitfinex.com/reference#rest-public-tickers
            var results = JsonConvert.DeserializeObject<List<List<object>>>(await bfResponse.Content.ReadAsStringAsync());
            var coins = ConvertToBitfinexCoins(results);

            var btc = coins.Find(c => c.Symbol == "tBTCUSD");
            var eth = coins.Find(c => c.Symbol == "tETHUSD");
            var ltc = coins.Find(c => c.Symbol == "tLTCUSD");
            var doge = coins.Find(c => c.Symbol == "tDOGUSD");
            var iot = coins.Find(c => c.Symbol == "tIOTUSD");
            var xmr = coins.Find(c => c.Symbol == "tXMRUSD");

            var message = new StringBuilder("__Current Price | Daily Change__\n");
            message.Append("BTC: ");
            message.Append(btc.LastPrice.ToString("0.00#"));
            message.Append(" | ");
            message.Append(btc.DailyChange.ToString("0.00#"));
            //message.Append(" (");
            //message.Append(btc.DailyChangePercentage.ToString("0.00#"));
            //message.Append("%)");
            message.Append("\n");

            message.Append("ETH: ");
            message.Append(eth.LastPrice.ToString("0.00#"));
            message.Append(" | ");
            message.Append(eth.DailyChange.ToString("0.00#"));
            //message.Append(" (");
            //message.Append(eth.DailyChangePercentage.ToString("0.00#"));
            //message.Append("%)");
            message.Append("\n");

            message.Append("DGE: ");
            // Doge is actually MDOGE with Bitfinex, so divide by 1M to get real price
            message.Append((doge.LastPrice / 1000000).ToString("0.00000#"));
            message.Append(" | ");
            message.Append((doge.DailyChange / 1000000).ToString("0.00000#"));
            //message.Append(" (");
            //message.Append(doge.DailyChangePercentage.ToString("0.00#"));
            //message.Append("%)");
            message.Append("\n");

            message.Append("LTC: ");
            message.Append(ltc.LastPrice.ToString("0.00#"));
            message.Append(" | ");
            message.Append(ltc.DailyChange.ToString("0.00#"));
            //message.Append(" (");
            //message.Append(ltc.DailyChangePercentage.ToString("0.00#"));
            //message.Append("%)");
            message.Append("\n");

            message.Append("IOT: ");
            message.Append(iot.LastPrice.ToString("0.00#"));
            message.Append(" | ");
            message.Append(iot.DailyChange.ToString("0.00#"));
            //message.Append(" (");
            //message.Append(iot.DailyChangePercentage.ToString("0.00#"));
            //message.Append("%)");
            message.Append("\n");

            message.Append("XMR: ");
            message.Append(xmr.LastPrice.ToString("0.00#"));
            message.Append(" | ");
            message.Append(xmr.DailyChange.ToString("0.00#"));
            //message.Append(" (");
            //message.Append(xmr.DailyChangePercentage.ToString("0.00#"));
            //message.Append("%)");

            await ReplyAsync(message.ToString());
        }

        private List<BitfinexCoin> ConvertToBitfinexCoins(List<List<object>> obj)
        {
            var coins = new List<BitfinexCoin>();

            foreach (var coin in obj)
            {
                coins.Add(new BitfinexCoin
                {
                    Symbol = (string)coin[0],
                    Bid = Convert.ToDecimal(coin[1]),
                    BidSize = Convert.ToDecimal(coin[2]),
                    Ask = Convert.ToDecimal(coin[3]),
                    AskSize = Convert.ToDecimal(coin[4]),
                    DailyChange = Convert.ToDecimal(coin[5]),
                    DailyChangePercentage = Convert.ToDecimal(coin[6]) * 100,
                    LastPrice = Convert.ToDecimal(coin[7]),
                    Volume = Convert.ToDecimal(coin[8]),
                    High = Convert.ToDecimal(coin[9]),
                    Low = Convert.ToDecimal(coin[10])
                });
            }

            return coins;
        }

        private class BitfinexCoin
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
