using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
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

        List<TwitterToCheck> _twittersToCheck;
        Timer _timer;

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
        }

        public async Task InitializeAsync()
        {
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

            _timer = new Timer(CheckForTweets, null, TimeSpan.Zero, TimeSpan.FromMinutes(4));
        }

        async void CheckForTweets(object _)
        {
            var updated = false;

            foreach (var ttc in _twittersToCheck)
            {
                var tweets = await TimelineAsync.GetUserTimeline(new UserIdentifier(ttc.TwitterId), new UserTimelineParameters
                {
                    ExcludeReplies = true,
                    IncludeRTS = ttc.IncludeRetweets,
                    SinceId = ttc.LatestTweetId,
                    MaximumNumberOfTweetsToRetrieve = 5
                });

                if (tweets?.Any() == true)
                {
                    updated = true;

                    tweets = tweets.GroupBy(x => x.Id)
                        .Select(x => x.First())
                        .OrderBy(x => x.CreatedAt);

                    ttc.LatestTweetId = tweets.Last().Id;

                    foreach (var tweet in tweets)
                    {
                        var channel = _discord.GetChannel((ulong)ttc.DiscordChannelId) as SocketTextChannel;
                        await channel.SendMessageAsync(tweet.Url);
                    }
                }
            }

            if (updated)
            {
                try
                {
                    using (var db = _provider.GetRequiredService<VbContext>())
                    {
                        db.TwittersToCheck.UpdateRange(_twittersToCheck);
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error updating database in Twitter service: check for tweets");
                }
            }
        }
    }
}
