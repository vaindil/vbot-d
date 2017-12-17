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
            HttpResponseMessage bfResponse;
            HttpResponseMessage btcResponse;
            HttpResponseMessage ethResponse;
            HttpResponseMessage ltcResponse;

            await Context.Channel.TriggerTypingAsync();
            try
            {
                bfResponse = await _httpClient.GetAsync("https://api.bitfinex.com/v2/tickers?symbols=tIOTUSD,tXMRUSD");
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

            // Bitfinex's API is terrible, they don't use key/value pairs because that would make too much sense
            // https://bitfinex.readme.io/v2/reference#rest-public-tickers
            var results = JsonConvert.DeserializeObject<List<List<object>>>(await bfResponse.Content.ReadAsStringAsync());
            var coins = ConvertToBitfinexCoins(results);

            var iot = coins.Find(c => c.Symbol == "tIOTUSD");
            var xmr = coins.Find(c => c.Symbol == "tXMRUSD");

            try
            {
                btcResponse = await _httpClient.GetAsync("https://api.gdax.com/products/BTC-USD/ticker");
                ethResponse = await _httpClient.GetAsync("https://api.gdax.com/products/ETH-USD/ticker");
                ltcResponse = await _httpClient.GetAsync("https://api.gdax.com/products/LTC-USD/ticker");
            }
            catch
            {
                await ReplyAsync("GDAX's API crapped out. Not my fault, sorry. Try again in a few seconds.");
                return;
            }

            if (!btcResponse.IsSuccessStatusCode || !ethResponse.IsSuccessStatusCode || !ltcResponse.IsSuccessStatusCode)
            {
                await ReplyAsync("GDAX's API crapped out. Not my fault, sorry. Try again in a few seconds.");
                return;
            }

            var btc = JsonConvert.DeserializeObject<GdaxCoin>(await btcResponse.Content.ReadAsStringAsync());
            var eth = JsonConvert.DeserializeObject<GdaxCoin>(await ethResponse.Content.ReadAsStringAsync());
            var ltc = JsonConvert.DeserializeObject<GdaxCoin>(await ltcResponse.Content.ReadAsStringAsync());

            var message = new StringBuilder();
            message.Append("BTC: ");
            message.Append(btc.Price.ToString("#.00#"));
            message.Append("\n");

            message.Append("ETH: ");
            message.Append(eth.Price.ToString("#.00#"));
            message.Append("\n");

            message.Append("LTC: ");
            message.Append(ltc.Price.ToString("#.00#"));
            message.Append("\n");

            message.Append("IOT: ");
            message.Append(iot.LastPrice.ToString("#.00#"));
            message.Append("\n");

            message.Append("XMR: ");
            message.Append(xmr.LastPrice.ToString("#.00#"));

            await ReplyAsync(message.ToString());
        }

        List<BitfinexCoin> ConvertToBitfinexCoins(List<List<object>> obj)
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
                    DailyChangePercentage = Convert.ToDecimal(coin[6]),
                    LastPrice = Convert.ToDecimal(coin[7]),
                    Volume = Convert.ToDecimal(coin[8]),
                    High = Convert.ToDecimal(coin[9]),
                    Low = Convert.ToDecimal(coin[10])
                });
            }

            return coins;
        }

        class GdaxCoin
        {
            public decimal Price { get; set; }
            public decimal Size { get; set; }
            public decimal Bid { get; set; }
            public decimal Ask { get; set; }
            public decimal Volume { get; set; }
            public DateTime Time { get; set; }
        }

        class BitfinexCoin
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
