using Discord.WebSocket;
using Google.Apis.YouTube.v3.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VainBot.Classes.YouTube;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class YouTubeService
    {
        private readonly DiscordSocketClient _discord;
        private readonly Google.Apis.YouTube.v3.YouTubeService _googleYtSvc;
        private readonly ILogger<YouTubeService> _logger;

        private readonly IServiceProvider _provider;

        private List<YouTubeChannelToCheck> _channels;

        private Timer _pollTimer;

        private readonly Regex _channelIdRegex = new Regex(@"https:\/\/(?:www.)?youtube.com\/channel\/([a-zA-Z0-9\-]+).*", RegexOptions.Compiled);
        private readonly Regex _usernameRegex = new Regex(@"https:\/\/(?:www.)?youtube.com\/user\/([a-zA-Z0-9\-]+).*", RegexOptions.Compiled);

        const ulong ROLEID = 458302101232156682;

        public YouTubeService(
            DiscordSocketClient discord,
            Google.Apis.YouTube.v3.YouTubeService googleYtSvc,
            ILogger<YouTubeService> logger,
            IServiceProvider provider)
        {
            _discord = discord;
            _googleYtSvc = googleYtSvc;
            _logger = logger;

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
                _logger.LogCritical(ex, "Error initializing YouTube service");
                return;
            }

            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }

            _pollTimer = new Timer(async (_) => await CheckYouTubeAsync(), null, 0, 600000);
        }

        public async Task CheckYouTubeAsync()
        {
            if (_channels == null || _channels.Count == 0)
                return;

            var channelChanged = false;

            foreach (var channel in _channels)
            {
                var request = _googleYtSvc.PlaylistItems.List("snippet");
                request.MaxResults = 10;
                request.PlaylistId = channel.YouTubePlaylistId;

                PlaylistItemListResponse response;

                try
                {
                    response = await request.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"YouTube check failed for playlist ID {channel.YouTubePlaylistId}, user {channel.Username}");
                    continue;
                }

                if (response.Items.Count == 0)
                    continue;

                var newest = response.Items.Select(i => i.Snippet).OrderByDescending(s => s.PublishedAt).First();
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
                    _logger.LogCritical(ex, "Error updating database in YouTube service: channel changed");
                }
            }
        }

        public async Task PostNewVideoAsync(YouTubeChannelToCheck channel, PlaylistItemSnippet video)
        {
            if (!(_discord.GetChannel((ulong)channel.DiscordChannelId) is SocketTextChannel discordChannel))
            {
                await RemoveChannelByIdAsync(channel.Id);
                _logger.LogError($"Discord channel does not exist: {channel.DiscordChannelId} in guild {channel.DiscordGuildId} " +
                    $"for YouTube channel {channel.Username} ({channel.YouTubeChannelId}).");
                return;
            }

            var role = discordChannel.Guild.GetRole(ROLEID);
            await role.ModifyAsync(x => x.Mentionable = true);

            var newMsg = await discordChannel.SendMessageAsync(
                $"{channel.DiscordMessageToPost} | https://www.youtube.com/watch?v={video.ResourceId.VideoId}");

            await role.ModifyAsync(x => x.Mentionable = false);

            if (channel.IsDeleted)
            {
                if (channel.DiscordMessageId.HasValue)
                {
                    var oldMsg = await discordChannel.GetMessageAsync((ulong)channel.DiscordMessageId.Value);
                    if (oldMsg != null)
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
                    _logger.LogCritical(ex, "Error updating database in YouTube service: post new video");
                }
            }
        }

        /// <summary>
        /// Gets all YouTube channels being checked for the given guild.
        /// </summary>
        /// <param name="guildId">ID of the guild for which to get YouTube channels</param>
        /// <returns>List of YouTube channels</returns>
        public List<YouTubeChannelToCheck> GetChannelsByGuild(ulong guildId)
        {
            return _channels
                .Where(c => c.DiscordGuildId == (long)guildId)
                .ToList();
        }

        /// <summary>
        /// Fills in all relevant YouTube API information for the given channel using the provided channel URL.
        /// Returns null if the channel URL is invalid.
        /// </summary>
        /// <param name="channelUrl">URL of the channel's YouTube page</param>
        /// <param name="channel">Channel object to fill in</param>
        /// <returns>Channel with information filled in if a success, otherwise null</returns>
        public async Task<YouTubeChannelToCheck> FillInChannelInformationAsync(string channelUrl, YouTubeChannelToCheck channel)
        {
            var request = _googleYtSvc.Channels.List("snippet,contentDetails");

            var matches = _usernameRegex.Match(channelUrl);
            if (matches.Success)
            {
                request.ForUsername = matches.Groups[1].Value;
            }
            else
            {
                matches = _channelIdRegex.Match(channelUrl);
                if (matches.Success)
                {
                    request.Id = matches.Groups[1].Value;
                }
                else
                {
                    return null;
                }
            }

            try
            {
                var response = await request.ExecuteAsync();

                if (response.Items.Count == 0)
                {
                    return null;
                }

                var ytChannel = response.Items[0];

                channel.Username = ytChannel.Snippet.Title;
                channel.YouTubeChannelId = ytChannel.Id;
                channel.YouTubePlaylistId = ytChannel.ContentDetails.RelatedPlaylists.Uploads;

                var playlistRequest = _googleYtSvc.PlaylistItems.List("snippet");
                playlistRequest.MaxResults = 10;
                playlistRequest.PlaylistId = channel.YouTubePlaylistId;

                var playlistResponse = await playlistRequest.ExecuteAsync();
                if (playlistResponse.Items.Count > 0)
                {
                    var newest = playlistResponse.Items.Select(i => i.Snippet).OrderByDescending(s => s.PublishedAt).First();
                    channel.LatestVideoId = newest.ResourceId.VideoId;
                    channel.LatestVideoUploadedAt = newest.PublishedAt;
                }

                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Error filling in YouTube information for channel with URL {channelUrl}.");
                return null;
            }
        }

        public async Task<bool> AddChannelAsync(YouTubeChannelToCheck channel)
        {
            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    var toCheck = await db.YouTubeChannelsToCheck
                        .FirstOrDefaultAsync(x => x.YouTubePlaylistId == channel.YouTubePlaylistId
                                               && x.DiscordChannelId == channel.DiscordChannelId);
                    if (toCheck != null)
                    {
                        toCheck.DiscordMessageToPost = channel.DiscordMessageToPost;
                        toCheck.IsDeleted = channel.IsDeleted;

                        db.YouTubeChannelsToCheck.Update(toCheck);
                    }
                    else
                    {
                        db.YouTubeChannelsToCheck.Add(channel);
                    }

                    await db.SaveChangesAsync();
                }

                _channels.Add(channel);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in YouTube service: adding new channel");
                return false;
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
                _logger.LogCritical(ex, "Error updating database in YouTube service: remove channel by ID");
            }
        }
    }
}
