using Discord;
using Discord.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using VainBot.Configs;
using VainBot.Preconditions;

namespace VainBot.Modules
{
    [FitzyGuild]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public class FitzyRecordModule : ModuleBase
    {
        private readonly FitzyConfig _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FitzyRecordModule> _logger;

        public FitzyRecordModule(IOptions<FitzyConfig> options, HttpClient httpClient, ILogger<FitzyRecordModule> logger)
        {
            _config = options.Value;
            _httpClient = httpClient;
            _logger = logger;
        }

        [Command("w")]
        [Alias("win", "wins")]
        public async Task Wins(int num = -1)
        {
            num = NormalizeNum(num);
            var success = await SendApiCallAsync(num, RecordType.wins);

            _logger.LogDebug($"Fitzy Record: {Context.Message.Author.Username} used !win {num}");

            await HandleReply(success);
        }

        [Command("l")]
        [Alias("loss", "losses")]
        public async Task Losses(int num = -1)
        {
            num = NormalizeNum(num);
            var success = await SendApiCallAsync(num, RecordType.losses);

            _logger.LogDebug($"Fitzy Record: {Context.Message.Author.Username} used !loss {num}");

            await HandleReply(success);
        }

        [Command("d")]
        [Alias("draw", "draws")]
        public async Task Draws(int num = -1)
        {
            num = NormalizeNum(num);
            var success = await SendApiCallAsync(num, RecordType.draws);

            _logger.LogDebug($"Fitzy Record: {Context.Message.Author.Username} used !draw {num}");

            await HandleReply(success);
        }

        [Command("clear")]
        [Alias("reset")]
        public async Task Clear()
        {
            var success = await SendApiCallAsync(0, RecordType.clear);

            _logger.LogDebug($"Fitzy Record: {Context.Message.Author.Username} used !clear");

            await HandleReply(success);
        }

        [Command("refresh")]
        public async Task Refresh()
        {
            var success = await SendApiCallAsync(0, RecordType.refresh);

            _logger.LogDebug($"Fitzy Record: {Context.Message.Author.Username} used !refresh");

            await HandleReply(success);
        }

        private async Task<bool> SendApiCallAsync(int num, RecordType type)
        {
            var url = $"{_config.ApiBaseUrl}/{type}";
            if (type != RecordType.clear && type != RecordType.refresh)
                url += $"/{num}";

            HttpMethod method;
            if (type != RecordType.refresh)
                method = HttpMethod.Put;
            else
                method = HttpMethod.Post;

            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization = new AuthenticationHeaderValue(_config.ApiSecret);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Fitzy API call failed. Status {response.StatusCode}, body: {body}");
            }

            return response.IsSuccessStatusCode;
        }

        private async Task HandleReply(bool success)
        {
            if (success)
                await ReplyAsync($"{Context.Message.Author.Mention}: Updated successfully");
            else
                await ReplyAsync($"{Context.Message.Author.Mention}: Error occurred while updating. " +
                    "The appropriate authorities have already been notified.");
        }

        private int NormalizeNum(int num)
        {
            if (num < -1)
                num = 0;
            else if (num > 99)
                num = 99;

            return num;
        }

        // breaking conventions and making this lowercase is so much nicer than making them uppercase and
        // converting to lowercase above
        private enum RecordType
        {
            wins,
            losses,
            draws,
            clear,
            refresh
        }
    }
}
