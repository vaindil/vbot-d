using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using VainBot.Classes.Twitter;
using VainBot.Configs;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class TwitterService
    {
        readonly DiscordSocketClient _discord;
        readonly ILogger<TwitterService> _logger;

        readonly TwitterConfig _config;
        readonly IServiceProvider _provider;

        readonly IMemoryCache _cache;

        List<TwitterToCheck> _twittersToCheck;
        IFilteredStream _stream;

        public TwitterService(
            DiscordSocketClient discord,
            ILogger<TwitterService> logger,
            IOptions<TwitterConfig> options,
            IServiceProvider provider)
        {
            _discord = discord;
            _logger = logger;
            _config = options.Value;

            _provider = provider;

            _cache = new MemoryCache(new MemoryCacheOptions());
        }

        public async Task InitializeAsync()
        {
            if (_stream != null)
            {
                _stream.MatchingTweetReceived -= HandleMatchingTweet;
                _stream.DisconnectMessageReceived -= HandleDisconnect;
                _stream.StreamStopped -= HandleStopped;

                _stream.StopStream();
                _stream = null;
            }

            Auth.ApplicationCredentials = new TwitterCredentials(
                _config.ConsumerKey, _config.ConsumerSecret,
                _config.AccessToken, _config.AccessTokenSecret);

            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    _twittersToCheck = await db.TwittersToCheck.ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error reading database in Twitter service: initialize");
                return;
            }

            if (_twittersToCheck.Count == 0)
                return;

            _stream = Stream.CreateFilteredStream();

            foreach (var toCheck in _twittersToCheck)
            {
                _stream.AddFollow(toCheck.TwitterId);
            }

            _stream.MatchingTweetReceived += HandleMatchingTweet;
            _stream.DisconnectMessageReceived += HandleDisconnect;
            _stream.StreamStopped += HandleStopped;

            await _stream.StartStreamMatchingAnyConditionAsync();
        }

        async void HandleMatchingTweet(object sender, MatchedTweetReceivedEventArgs e)
        {
            if (_cache.TryGetValue(e.Tweet.Id, out _))
                return;

            _cache.Set(e.Tweet.Id, true, CacheEntryOptions);

            if (e.Tweet.InReplyToUserId.HasValue)
                return;

            var toCheck = _twittersToCheck.Find(t => t.TwitterId == e.Tweet.CreatedBy.Id);
            if (toCheck == null)
                return;

            if (e.Tweet.IsRetweet && !toCheck.IncludeRetweets)
                return;

            var channel = _discord.GetChannel((ulong)toCheck.DiscordChannelId) as SocketTextChannel;
            await channel.SendMessageAsync(e.Tweet.Url);
        }

        async void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            //await _logSvc.LogMessageAsync(LogSeverity.Warning, "Twitter stream disconnected. Restarting.");
            await _stream.StartStreamMatchingAnyConditionAsync();
        }

        async void HandleStopped(object sender, StreamExceptionEventArgs e)
        {
            //await _logSvc.LogMessageAsync(LogSeverity.Warning, "Twitter stream stopped. Restarting.");
            await _stream.StartStreamMatchingAnyConditionAsync();
        }

        static MemoryCacheEntryOptions CacheEntryOptions
        {
            get
            {
                return new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
                    Priority = CacheItemPriority.NeverRemove
                };
            }
        }
    }
}
