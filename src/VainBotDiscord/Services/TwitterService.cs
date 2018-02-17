using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Events;
using Tweetinvi.Models;
using Tweetinvi.Streaming;
using VainBotDiscord.Classes;

namespace VainBotDiscord.Services
{
    public class TwitterService
    {
        readonly DiscordSocketClient _discord;
        readonly IConfiguration _config;
        IServiceProvider _provider;

        List<TwitterToCheck> _twittersToCheck;
        IFilteredStream _stream;

        public TwitterService(
            DiscordSocketClient discord,
            IConfiguration config,
            IServiceProvider provider)
        {
            _discord = discord;
            _config = config;
            _provider = provider;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;

            Auth.ApplicationCredentials = new TwitterCredentials(
                _config["twitter_consumer_key"], _config["twitter_consumer_secret"],
                _config["twitter_access_token"], _config["twitter_access_token_secret"]);

            using (var db = _provider.GetRequiredService<VbContext>())
            {
                _twittersToCheck = await db.TwittersToCheck.ToListAsync();
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
            if (e.Tweet.InReplyToUserId.HasValue)
                return;

            var toCheck = _twittersToCheck.Find(t => t.TwitterId == e.Tweet.CreatedBy.Id);
            if (toCheck == null)
                return;

            if (e.Tweet.IsRetweet && !toCheck.IncludeRetweets)
                return;

            var channel = _discord.GetChannel(toCheck.DiscordChannelId) as SocketTextChannel;
            await channel.SendMessageAsync(e.Tweet.Url);
        }

        async void HandleDisconnect(object sender, DisconnectedEventArgs e)
        {
            await _stream.StartStreamMatchingAnyConditionAsync();
        }

        async void HandleStopped(object sender, StreamExceptionEventArgs e)
        {
            await _stream.StartStreamMatchingAnyConditionAsync();
        }
    }
}
