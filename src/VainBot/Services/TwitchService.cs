﻿using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using VainBot.Classes.Twitch;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class TwitchService
    {
        private readonly DiscordSocketClient _discord;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<TwitchService> _logger;

        private readonly IServiceProvider _provider;

        private List<TwitchStreamToCheck> _streamsToCheck;
        private List<TwitchLiveStream> _liveStreams;

        private string _oauthToken;

        private Timer _pollTimer;
        private Timer _oauthRefreshTimer;

        const string NOGAMESETURL = "https://vaindil.xyz/misc/nogame.png";
        const ulong ROLEID = 458302101232156682;

        public TwitchService(
            DiscordSocketClient discord,
            HttpClient httpClient,
            IConfiguration config,
            ILogger<TwitchService> logger,
            IServiceProvider provider)
        {
            _discord = discord;
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            _provider = provider;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Twitch service");
            await GetTwitchTokensAsync();

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                _streamsToCheck = await db.StreamsToCheck.AsQueryable().ToListAsync();
                _liveStreams = await db.TwitchLiveStreams.AsQueryable().ToListAsync();

                _logger.LogInformation("_streamsToCheck and _liveStreams initialized");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error initializing Twitch service");
                return;
            }

            _pollTimer?.Dispose();
            _pollTimer = new Timer(async (_) => await CheckStreamsAsync(), null, 0, 60000);

            _logger.LogInformation("Twitch service initialization complete");
        }

        /// <summary>
        /// Checks all streams currently in memory. The Twitch API supports up to 100 user IDs per request.
        /// </summary>
        public async Task CheckStreamsAsync()
        {
            if (_streamsToCheck == null || _streamsToCheck.Count == 0)
                return;

            var userIds = _streamsToCheck
                .GroupBy(s => s.TwitchId)
                .Select(s => s.First().TwitchId);

            var userIdStreamString = string.Join("&user_id=", userIds);

            var streamRequest = GetRequestMessage();
            streamRequest.RequestUri = new Uri($"https://api.twitch.tv/helix/streams?user_id={userIdStreamString}");
            streamRequest.Method = HttpMethod.Get;

            var streamResponse = await _httpClient.SendAsync(streamRequest);

            try
            {
                await ThrowIfResponseInvalidAsync(streamResponse);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error executing Twitch stream check");
                return;
            }

            var liveStreams =
                JsonConvert.DeserializeObject<TwitchStreamResponse>(await streamResponse.Content.ReadAsStringAsync()).Streams;

            var games = new List<TwitchGame>();
            var users = new List<TwitchUser>();

            if (liveStreams.Count > 0)
            {
                var gameIds = liveStreams
                    .GroupBy(s => s.GameId)
                    .Select(s => s.First().GameId)
                    .Where(g => !string.IsNullOrEmpty(g));

                var gameIdString = string.Join("&id=", gameIds);

                var gameRequest = GetRequestMessage();
                gameRequest.RequestUri = new Uri($"https://api.twitch.tv/helix/games?id={gameIdString}");
                gameRequest.Method = HttpMethod.Get;

                var gameResponse = await _httpClient.SendAsync(gameRequest);

                try
                {
                    await ThrowIfResponseInvalidAsync(gameResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error executing Twitch game request");
                    return;
                }

                games = JsonConvert.DeserializeObject<TwitchGameResponse>(await gameResponse.Content.ReadAsStringAsync()).Games;

                userIds = liveStreams
                    .GroupBy(s => s.UserId)
                    .Select(s => s.First().UserId);

                var userIdUserString = string.Join("&id=", userIds);

                var userRequest = GetRequestMessage();
                userRequest.RequestUri = new Uri($"https://api.twitch.tv/helix/users?id={userIdUserString}");
                userRequest.Method = HttpMethod.Get;

                var userResponse = await _httpClient.SendAsync(userRequest);

                try
                {
                    await ThrowIfResponseInvalidAsync(userResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error executing Twitch user request");
                    return;
                }

                users = JsonConvert.DeserializeObject<TwitchUserResponse>(await userResponse.Content.ReadAsStringAsync()).Users;
            }

            var newlyOnline = new List<TwitchLiveStream>();
            var newlyOffline = new List<TwitchLiveStream>(_liveStreams);
            var stillOnline = new List<TwitchLiveStream>();

            foreach (var stream in liveStreams)
            {
                // first, check if the streamer WAS online as of the last check.
                var existingStream = _liveStreams.Find(l => l.TwitchUserId == stream.UserId || l.TwitchStreamId == stream.Id);

                var user = users.Find(u => u.Id == stream.UserId);
                var game = games.Find(g => g.Id == stream.GameId);
                if (game == null)
                {
                    game = new TwitchGame
                    {
                        Id = "0",
                        Name = "(none)",
                        BoxArtUrl = NOGAMESETURL
                    };
                }

                // if the streamer WAS online and STILL IS online, then they're not newly offline.
                // they also belong in stillOnline.
                if (existingStream != null)
                {
                    newlyOffline.RemoveAll(t => t.TwitchUserId == stream.UserId || t.TwitchStreamId == stream.Id);

                    existingStream.TwitchStreamId = stream.Id;
                    existingStream.TwitchDisplayName = user.DisplayName;
                    existingStream.GameId = stream.GameId;
                    existingStream.GameName = game.Name;
                    existingStream.ThumbnailUrl = stream.ThumbnailUrl;
                    existingStream.ViewerCount = stream.ViewerCount;
                    existingStream.ProfileImageUrl = user.ProfileImageUrl;

                    stillOnline.Add(existingStream);
                }

                // the streamer WAS NOT online, so they're newly online. also verify that it's not a vodcast.
                else if (stream.Type != TwitchStreamType.Vodcast && !_liveStreams.Any(l => l.TwitchStreamId == stream.Id))
                {
                    var liveStream = new TwitchLiveStream
                    {
                        StartedAt = stream.StartedAt,
                        TwitchStreamId = stream.Id,
                        TwitchUserId = stream.UserId,
                        TwitchLogin = user.Login,
                        TwitchDisplayName = user.DisplayName,
                        GameId = stream.GameId,
                        Title = stream.Title,
                        ViewerCount = stream.ViewerCount,
                        GameName = game.Name,
                        ThumbnailUrl = stream.ThumbnailUrl,
                        ProfileImageUrl = user.ProfileImageUrl
                    };

                    newlyOnline.Add(liveStream);
                    _liveStreams.Add(liveStream);
                }
            }

            var actuallyNewlyOffline = new List<TwitchLiveStream>();

            foreach (var n in newlyOffline)
            {
                if (!n.FirstOfflineAt.HasValue)
                {
                    n.FirstOfflineAt = DateTimeOffset.UtcNow;
                }
                else if (n.FirstOfflineAt.Value <= DateTimeOffset.UtcNow.AddMinutes(-5))
                {
                    _liveStreams.Remove(n);
                    actuallyNewlyOffline.Add(n);
                }
            }

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                db.TwitchLiveStreams.AddRange(newlyOnline);
                db.TwitchLiveStreams.UpdateRange(stillOnline);
                db.TwitchLiveStreams.UpdateRange(newlyOffline);
                db.TwitchLiveStreams.RemoveRange(actuallyNewlyOffline);

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database during Twitch check");
                return;
            }

            await HandleNewlyOnlineStreamsAsync(newlyOnline);
            await HandleStillOnlineStreamsAsync(stillOnline);
            await HandleNewlyOfflineStreamsAsync(actuallyNewlyOffline);
        }

        public async Task HandleNewlyOnlineStreamsAsync(List<TwitchLiveStream> streams)
        {
            if (streams.Count == 0)
                return;

            var toCheckUpdated = new List<TwitchStreamToCheck>();

            foreach (var toCheck in _streamsToCheck)
            {
                var stream = streams.Find(s => s.TwitchUserId == toCheck.TwitchId);
                if (stream == null)
                    continue;

                _logger.LogInformation($"Stream newly live | ID: {toCheck.Id} | Twitch name: {stream.TwitchDisplayName}" +
                    $" | channel: {toCheck.ChannelId} | embedded: {toCheck.IsEmbedded}");

                Embed embed = null;
                if (toCheck.IsEmbedded)
                    embed = CreateEmbed(stream);

                if (!(_discord.GetChannel((ulong)toCheck.ChannelId) is SocketTextChannel channel))
                {
                    _logger.LogError($"Channel does not exist: {toCheck.ChannelId} in guild {toCheck.GuildId} for streamer {toCheck.Username}");
                    await RemoveStreamByIdAsync(toCheck.Id);
                    return;
                }

                var role = channel.Guild.GetRole(ROLEID);
                await role.ModifyAsync(x => x.Mentionable = true);

                RestUserMessage message;
                try
                {
                    message = await channel.SendMessageAsync(toCheck.MessageToPost, embed: embed);
                }
                catch (HttpException ex)
                {
                    _logger.LogError(ex, $"Exception when trying to send stream for Twitch channel {toCheck.Username} to Discord channel {toCheck.ChannelId}");
                    if (ex.DiscordCode == 50013)
                    {
                        _logger.LogError("Bot does not have permission to post, removing Twitch entry.");
                        await RemoveStreamByIdAsync(toCheck.Id);
                    }

                    await role.ModifyAsync(x => x.Mentionable = false);
                    return;
                }

                toCheck.CurrentMessageId = (long)message.Id;
                toCheckUpdated.Add(toCheck);

                await role.ModifyAsync(x => x.Mentionable = false);
            }

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                db.StreamsToCheck.UpdateRange(toCheckUpdated);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in Twitch service: newly online");
            }
        }

        public async Task HandleStillOnlineStreamsAsync(List<TwitchLiveStream> streams)
        {
            if (streams.Count == 0)
                return;

            foreach (var toCheck in _streamsToCheck.Where(s => s.IsEmbedded && s.CurrentMessageId.HasValue))
            {
                var stream = streams.Find(s => s.TwitchUserId == toCheck.TwitchId);
                if (stream == null)
                    continue;

                var embed = CreateEmbed(stream);
                var channel = _discord.GetChannel((ulong)toCheck.ChannelId) as SocketTextChannel;
                if (await channel.GetMessageAsync((ulong)toCheck.CurrentMessageId.Value) is RestUserMessage message)
                    await message.ModifyAsync(m => m.Embed = embed);
            }
        }

        public async Task HandleNewlyOfflineStreamsAsync(List<TwitchLiveStream> streams)
        {
            if (streams.Count == 0)
                return;

            var toCheckUpdated = new List<TwitchStreamToCheck>();

            foreach (var toCheck in _streamsToCheck.Where(s => s.CurrentMessageId.HasValue))
            {
                var stream = streams.Find(s => s.TwitchUserId == toCheck.TwitchId);
                if (stream == null)
                    continue;

                if (toCheck.IsDeleted)
                {
                    try
                    {
                        var channel = _discord.GetChannel((ulong)toCheck.ChannelId) as SocketTextChannel;
                        var message = await channel.GetMessageAsync((ulong)toCheck.CurrentMessageId.Value) as RestUserMessage;
                        await message.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Could not delete Twitch stream message.");
                    }
                }

                toCheck.CurrentMessageId = null;
                toCheckUpdated.Add(toCheck);
            }

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                db.StreamsToCheck.UpdateRange(toCheckUpdated);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in Twitch service: newly offline");
            }
        }

        private Embed CreateEmbed(TwitchLiveStream stream)
        {
            var now = DateTime.UtcNow;
            var cacheBuster =
                now.Year.ToString() +
                now.Month.ToString() +
                now.Day.ToString() +
                now.Hour.ToString() +
                ((now.Minute / 10) % 10).ToString();

            var author = new EmbedAuthorBuilder
            {
                Name = stream.TwitchDisplayName,
                IconUrl = stream.ProfileImageUrl,
                Url = $"https://www.twitch.tv/{stream.TwitchLogin}"
            };

            var playingField = new EmbedFieldBuilder
            {
                Name = "Playing",
                Value = stream.GameName,
                IsInline = true
            };

            var viewerCountField = new EmbedFieldBuilder
            {
                Name = "Viewers",
                Value = stream.ViewerCount,
                IsInline = true
            };

            var embed = new EmbedBuilder
            {
                // https://www.twitch.tv/p/brand/
                Color = new Color(100, 65, 164),
                Author = author,
                Title = stream.Title,
                ImageUrl = stream.ThumbnailUrl.Replace("{width}", "640").Replace("{height}", "360") + $"?{cacheBuster}",
                Url = author.Url
            };

            embed.AddField(playingField);
            embed.AddField(viewerCountField);

            return embed.Build();
        }

        /// <summary>
        /// Gets all streams for the given guild
        /// </summary>
        /// <param name="guildId">ID of the guild</param>
        /// <returns>List of streams</returns>
        public List<TwitchStreamToCheck> GetStreamsByGuild(ulong guildId)
        {
            return _streamsToCheck
                .Where(s => s.GuildId == (long)guildId)
                .ToList();
        }

        /// <summary>
        /// Adds a stream to be checked
        /// </summary>
        /// <param name="stream">Stream to add</param>
        public async Task<bool> AddStreamAsync(TwitchStreamToCheck stream)
        {
            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    var toCheck = await db.StreamsToCheck.AsQueryable()
                        .FirstOrDefaultAsync(x => x.TwitchId == stream.TwitchId && x.ChannelId == stream.ChannelId);
                    if (toCheck != null)
                    {
                        toCheck.IsDeleted = stream.IsDeleted;
                        toCheck.IsEmbedded = stream.IsEmbedded;
                        toCheck.MessageToPost = stream.MessageToPost;

                        db.StreamsToCheck.Update(toCheck);
                    }
                    else
                    {
                        db.StreamsToCheck.Add(stream);
                    }

                    await db.SaveChangesAsync();
                }

                _streamsToCheck.Add(stream);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in Twitch service: adding new stream");
                return false;
            }
        }

        /// <summary>
        /// Removes a stream that's currently being checked
        /// </summary>
        /// <param name="id">ID of the entry to remove</param>
        public async Task RemoveStreamByIdAsync(int id)
        {
            var stream = _streamsToCheck.Find(s => s.Id == id);
            if (stream == null)
                return;

            _streamsToCheck.Remove(stream);

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                var s = await db.StreamsToCheck.FindAsync(id);
                if (s != null)
                {
                    db.StreamsToCheck.Remove(s);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in Twitch service: removing stream");
            }
        }

        /// <summary>
        /// Gets an HttpRequestMessage with credentials specified that can be used to query the Twitch API.
        /// </summary>
        /// <returns>HttpRequestMessage</returns>
        public HttpRequestMessage GetRequestMessage()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add("Client-ID", _config["twitch_client_id"]);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _oauthToken);
            return request;
        }

        /// <summary>
        /// Throws an InvalidOperationException for the provided response if
        /// the response was an error.
        /// </summary>
        /// <param name="response">HttpResponseMessage to check</param>
        public async Task ThrowIfResponseInvalidAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<TwitchErrorResponse>(body);

            throw new InvalidOperationException(
                $"Twitch request failed with error code {error.Status}, {error.Error}. " +
                $"Message: {error.Message}");
        }

        /// <summary>
        /// Gets the display name and ID of the given Twitch username.
        /// </summary>
        /// <param name="username">Username to look up</param>
        /// <returns>ID and display name of user. ID will be "-1" on error.</returns>
        public async Task<(string Id, string DisplayName)> GetUserIdAsync(string username)
        {
            var request = GetRequestMessage();
            request.RequestUri = new Uri($"https://api.twitch.tv/helix/users?login={username}");
            request.Method = HttpMethod.Get;

            var response = await _httpClient.SendAsync(request);
            try
            {
                await ThrowIfResponseInvalidAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error in Twitch service: user ID request");
                return ("-1", "An error occurred when getting the ID. Please try again, or yell at vaindil.");
            }

            var users = JsonConvert.DeserializeObject<TwitchUserResponse>(await response.Content.ReadAsStringAsync());
            if (users.Users.Count == 0)
                return ("-1", $"The user **{username}** does not exist.");

            return (users.Users[0].Id, users.Users[0].DisplayName);
        }

        /// <summary>
        /// Gets or refreshes the current auth token.
        /// </summary>
        private async Task GetTwitchTokensAsync()
        {
            _logger.LogInformation("Beginning to get Twitch tokens");

            var url = "https://id.twitch.tv/oauth2/token?grant_type=client_credentials";
            url += $"&client_id={_config["twitch_client_id"]}";
            url += $"&client_secret={_config["twitch_client_secret"]}";

            var response = await _httpClient.PostAsync(url, null);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error authenticating with Twitch: {body}");
                throw new Exception();
            }

            _logger.LogInformation("Successfully called Twitch token endpoint");

            var tokenResp = JsonConvert.DeserializeObject<TwitchTokenResponse>(body);
            _oauthToken = tokenResp.AccessToken;

            _logger.LogInformation("Successfully set Twitch token");

            if (tokenResp.ExpiresInSeconds > 2592000)
                tokenResp.ExpiresInSeconds = 2592000;

            _oauthRefreshTimer?.Dispose();
            _oauthRefreshTimer = new Timer(async (_) => await GetTwitchTokensAsync(), null,
                TimeSpan.FromSeconds(tokenResp.ExpiresInSeconds - 600), TimeSpan.FromMilliseconds(-1));

            _logger.LogInformation("Finished getting Twitch token");
        }
    }
}
