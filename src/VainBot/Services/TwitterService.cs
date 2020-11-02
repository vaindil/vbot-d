using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;
using VainBot.Classes.Twitter;
using VainBot.Configs;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class TwitterService
    {
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<TwitterService> _logger;
        private readonly TwitterClient _twitterClient;

        private readonly TwitterConfig _config;
        private readonly IServiceProvider _provider;

        private List<TwitterToCheck> _twittersToCheck;
#pragma warning disable IDE0052 // Remove unread private members
        private Timer _timer;
#pragma warning restore IDE0052 // Remove unread private members

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

            _twitterClient = new TwitterClient(
                new TwitterCredentials(
                    _config.ConsumerKey, _config.ConsumerSecret,
                    _config.AccessToken, _config.AccessTokenSecret));
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var db = _provider.GetRequiredService<VbContext>();
                _twittersToCheck = await db.TwittersToCheck.AsQueryable().ToListAsync();
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

        private async void CheckForTweets(object _)
        {
            var updated = false;

            foreach (var ttc in _twittersToCheck)
            {
                var tweets = await _twitterClient.Timelines.GetUserTimelineAsync(new GetUserTimelineParameters(ttc.TwitterId)
                {
                    ExcludeReplies = true,
                    IncludeRetweets = ttc.IncludeRetweets,
                    SinceId = ttc.LatestTweetId,
                    PageSize = 5
                });

                if (tweets?.Any() == true)
                {
                    updated = true;

                    var filteredTweets = tweets.GroupBy(x => x.Id)
                        .Select(x => x.First())
                        .OrderBy(x => x.CreatedAt);

                    ttc.LatestTweetId = filteredTweets.Last().Id;

                    foreach (var tweet in filteredTweets)
                    {
                        ttc.TwitterUsername = tweet.CreatedBy.ScreenName;

                        var channel = _discord.GetChannel((ulong)ttc.DiscordChannelId) as SocketTextChannel;
                        await channel.SendMessageAsync(tweet.Url);
                    }
                }
            }

            if (updated)
            {
                try
                {
                    using var db = _provider.GetRequiredService<VbContext>();

                    db.TwittersToCheck.UpdateRange(_twittersToCheck);
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Error updating database in Twitter service: check for tweets");
                }
            }
        }

        public async Task<bool> AddTwitterToCheckAsync(TwitterToCheck toCheck)
        {
            if (_twittersToCheck.Any(x => x.TwitterId == toCheck.TwitterId && x.DiscordChannelId == toCheck.DiscordChannelId))
                return true;

            var latestTweets = await _twitterClient.Timelines.GetUserTimelineAsync(new GetUserTimelineParameters(toCheck.TwitterId)
            {
                ExcludeReplies = true,
                IncludeRetweets = toCheck.IncludeRetweets,
                PageSize = 1
            });

            var latestTweet = latestTweets.FirstOrDefault();
            if (latestTweet != null)
                toCheck.LatestTweetId = latestTweet.Id;

            try
            {
                using (var db = _provider.GetRequiredService<VbContext>())
                {
                    db.TwittersToCheck.Add(toCheck);
                    await db.SaveChangesAsync();
                }

                _twittersToCheck.Add(toCheck);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in Twitter service: adding new to check");
                return false;
            }
        }

        public async Task RemoveTwitterToCheckByIdAsync(int id)
        {
            var toCheck = _twittersToCheck.Find(x => x.Id == id);
            if (toCheck == null)
                return;

            _twittersToCheck.Remove(toCheck);

            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                var t = await db.TwittersToCheck.FindAsync(id);
                if (t != null)
                {
                    db.TwittersToCheck.Remove(t);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in Twitter service: removing to check");
            }
        }

        /// <summary>
        /// Gets a Twitter user's information from their username.
        /// </summary>
        /// <param name="username">Username of the account to get the ID of</param>
        /// <returns>ID and username of the account if it exists, otherwise null</returns>
        public async Task<(long? id, string username)> GetUserInfoAsync(string username)
        {
            try
            {
                var user = await _twitterClient.Users.GetUserAsync(username);
                return (user.Id, user.ScreenName);
            }
            catch
            {
                return (null, null);
            }
        }

        public List<TwitterToCheck> GetTimelinesByGuild(ulong guildId)
        {
            return _twittersToCheck
                .Where(x => x.DiscordGuildId == (long)guildId)
                .ToList();
        }
    }
}
