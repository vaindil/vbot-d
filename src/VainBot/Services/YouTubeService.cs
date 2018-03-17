using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VainBot.Classes;

namespace VainBot.Services
{
    public class YouTubeService
    {
        readonly DiscordSocketClient _discord;
        readonly HttpClient _httpClient;
        readonly IConfiguration _config;

        readonly LogService _logSvc;
        readonly IServiceProvider _provider;

        List<YouTubeChannelToCheck> _channels;

        Timer _pollTimer;

        public YouTubeService(
            DiscordSocketClient discord,
            HttpClient httpClient,
            IConfiguration config,
            LogService logSvc,
            IServiceProvider provider)
        {
            _discord = discord;
            _httpClient = httpClient;
            _config = config;

            _logSvc = logSvc;
            _provider = provider;
        }

        public async Task InitializeAsync()
        {
            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    _channels = await db.YouTubeChannelsToCheck.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                await _logSvc.LogExceptionAsync(ex);
                return;
            }

            _pollTimer = new Timer(async (e) => await CheckYouTubeAsync(), null, 0, 60000);
        }

        public async Task CheckYouTubeAsync()
        {
            if (_channels == null || _channels.Count == 0)
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
                    await _logSvc.LogMessageAsync(LogSeverity.Critical,
                        $"YouTube check failed for playlist ID {channel.YouTubePlaylistId}, user {channel.Username}");
                    continue;
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
                try
                {
                    using (var db = _provider.GetRequiredService<VbContext>())
                    {
                        db.YouTubeChannelsToCheck.UpdateRange(_channels);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    await _logSvc.LogExceptionAsync(ex);
                }
            }
        }

        public async Task PostNewVideoAsync(YouTubeChannelToCheck channel, YouTubeVideoSnippet video)
        {
            var discordChannel = _discord.GetChannel((ulong)channel.DiscordChannelId) as SocketTextChannel;
            if (discordChannel == null)
            {
                await RemoveChannelByIdAsync(channel.Id);
                await _logSvc.LogMessageAsync(LogSeverity.Warning,
                    $"Discord channel does not exist: {channel.DiscordChannelId} in guild {channel.DiscordGuildId} " +
                    $"for YouTube channel {channel.Username} ({channel.YouTubeChannelId}). Removing entry.");
                return;
            }

            var newMsg = await discordChannel.SendMessageAsync(
                $"{channel.DiscordMessageToPost} | https://www.youtube.com/watch?v={video.ResourceId.VideoId}");

            if (channel.IsDeleted)
            {
                if (channel.DiscordMessageId.HasValue)
                {
                    var oldMsg = await discordChannel.GetMessageAsync((ulong)channel.DiscordMessageId.Value);
                    await oldMsg.DeleteAsync();
                }

                channel.DiscordMessageId = (long)newMsg.Id;

                try
                {
                    using (var db = _provider.GetRequiredService<VbContext>())
                    {
                        db.YouTubeChannelsToCheck.Update(channel);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    await _logSvc.LogExceptionAsync(ex);
                }
            }
        }

        public async Task RemoveChannelByIdAsync(int id)
        {
            var channel = _channels.Find(s => s.Id == id);
            if (channel == null)
                return;

            _channels.Remove(channel);

            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    var c = await db.YouTubeChannelsToCheck.FindAsync(id);
                    if (c != null)
                    {
                        db.YouTubeChannelsToCheck.Remove(c);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await _logSvc.LogExceptionAsync(ex);
            }
        }
    }
}
