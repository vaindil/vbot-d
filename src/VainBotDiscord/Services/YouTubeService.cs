using Discord.WebSocket;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using VainBotDiscord.Classes;

namespace VainBotDiscord.Services
{
    public class YouTubeService
    {
        readonly DiscordSocketClient _discord;
        readonly HttpClient _httpClient;
        readonly IConfiguration _config;
        IServiceProvider _provider;

        List<YouTubeChannelToCheck> _channels;

        public YouTubeService(
            DiscordSocketClient discord,
            HttpClient httpClient,
            IConfiguration config,
            IServiceProvider provider)
        {
            _discord = discord;
            _httpClient = httpClient;
            _config = config;
            _provider = provider;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;

            using (var db = _provider.GetRequiredService<VbContext>())
            {
                _channels = await db.YouTubeChannelsToCheck.ToListAsync();
            }

            RecurringJob.AddOrUpdate("YouTubeCheck", () => CheckYouTubeAsync(), Cron.Minutely);
        }

        public async Task CheckYouTubeAsync()
        {
            if (_channels.Count == 0)
                return;

            var channelChanged = false;

            foreach (var channel in _channels)
            {
                var response = await _httpClient.GetAsync(
                    "https://www.googleapis.com/youtube/v3/playlistItems" +
                    $"?playlistId={channel.YouTubePlaylistId}" +
                    "&maxResults=5" +
                    "&part=snippet" +
                    $"&key={_config["youtube_api_key"]}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"YouTube check failed for playlist ID {channel.YouTubePlaylistId}, user {channel.Username}");
                }

                var content = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<YouTubePlaylistItemsResponse>(content);

                if (result.Items.Count == 0)
                    continue;

                var newest = result.Items.Select(i => i.Snippet).OrderByDescending(s => s.PublishedAt).First();
                if (channel.LatestVideoUploadedAt.HasValue && newest.PublishedAt <= channel.LatestVideoUploadedAt.Value)
                    continue;

                channel.LatestVideoId = newest.ResourceId.VideoId;
                channel.LatestVideoUploadedAt = newest.PublishedAt;
                channelChanged = true;

                await PostNewVideoAsync(channel, newest);
            }

            if (channelChanged)
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    db.YouTubeChannelsToCheck.UpdateRange(_channels);
                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task PostNewVideoAsync(YouTubeChannelToCheck channel, YouTubeVideoSnippet video)
        {
            var discordChannel = _discord.GetChannel(channel.DiscordChannelId) as SocketTextChannel;
            await discordChannel.SendMessageAsync(
                $"{channel.DiscordMessageToPost} | https://www.youtube.com/watch?v={video.ResourceId.VideoId}");
        }
    }
}
