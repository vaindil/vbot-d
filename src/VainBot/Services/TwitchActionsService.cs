using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PureWebSockets;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using VainBot.Classes.Twitch;
using VainBot.Classes.Users;
using VainBot.Configs;

namespace VainBot.Services
{
    public class TwitchActionsService
    {
        private readonly FitzyConfig _config;
        private readonly UserService _userSvc;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<TwitchActionsService> _logger;

        private PureWebSocket _ws;

        private bool _isLive = false;

        public TwitchActionsService(
            IOptions<FitzyConfig> options,
            UserService userSvc,
            DiscordSocketClient discord,
            ILogger<TwitchActionsService> logger)
        {
            _config = options.Value;
            _userSvc = userSvc;
            _discord = discord;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            while (_ws?.State != WebSocketState.Open)
            {
                InitializeWebSocket();

                var success = false;
                try
                {
                    success = _ws.Connect();
                }
                catch
                {
                    _ws.Dispose(false);
                    _ws = null;
                }

                if (success)
                    break;

                await Task.Delay(1500);
            }
        }

        private void InitializeWebSocket()
        {
            var wsOptions = new PureWebSocketOptions
            {
                MyReconnectStrategy = new ReconnectStrategy(1000, 2500),
            };

            _ws = new PureWebSocket($"{_config.TwitchActionSocketUrl}?key={_config.ApiSecret}", wsOptions);
            _ws.OnStateChanged += StateChanged;
            _ws.OnMessage += HandleMessageAsync;
        }

        private void StateChanged(WebSocketState newState, WebSocketState oldState)
        {
            _logger.LogInformation($"Twitch action websocket state change: {oldState} to {newState}");
        }

        private async void HandleMessageAsync(string message)
        {
            var actionChannel = _discord.GetChannel(480178651837628436) as SocketTextChannel;

            // i know this WS code is ugly and hacky, i'm messing around with twitch webhooks.
            // this should really just use the normal stream checking from TwitchService but that's no fun.

            StreamChangedNotification notification = null;
            if (message.StartsWith('{'))
            {
                try
                {
                    notification = JsonConvert.DeserializeObject<StreamChangedNotification>(message);
                }
                catch
                {
                }
            }

            if (notification != null)
            {
                _logger.LogInformation("New Twitch webhook websocket message received");

                Embed twitchEmbed = null;
                if (notification.Status == "live" && !_isLive)
                {
                    _isLive = true;

                    var startedAt = notification.StartedAt ?? DateTimeOffset.UtcNow;

                    twitchEmbed = new EmbedBuilder()
                        .WithColor(100, 65, 164)
                        .WithTitle("Twitch")
                        .WithDescription("Fitzy just went live.")
                        .WithFooter(startedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"))
                        .Build();
                }
                else if (notification.Status == "offline")
                {
                    _isLive = false;

                    twitchEmbed = new EmbedBuilder()
                        .WithColor(100, 65, 164)
                        .WithTitle("Twitch")
                        .WithDescription("Fitzy just went offline.")
                        .WithFooter(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"))
                        .Build();
                }

                if (twitchEmbed != null)
                    await actionChannel.SendMessageAsync(embed: twitchEmbed);

                return;
            }

            _logger.LogInformation("New websocket message received: " + message);

            if (!message.StartsWith("ACTION"))
                return;

            var parts = message.Split(' ', 6, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
                return;

            var modUsername = parts[1];
            var userUsername = parts[2];
            var action = parts[3];
            var duration = int.Parse(parts[4]);
            var reason = parts[5];

            var discordMod = await _userSvc.GetDiscordUserByTwitchUsername(modUsername);
            if (discordMod == null)
            {
                await actionChannel.SendMessageAsync($"The user {userUsername} had action `{action}` taken against them by Twitch mod " +
                    $"{modUsername} with a duration of {duration} seconds, but I don't have that mod in my system. This action " +
                    "has therefore not been recorded against the user.");
                return;
            }

            if (!Enum.TryParse(typeof(ActionTakenType), action, true, out var actionTakenType))
            {
                await actionChannel.SendMessageAsync($"The user {userUsername} had action `{action}` taken against them by " +
                    $"{discordMod.Mention} with a duration of {duration} seconds, but I don't have a record of that type " +
                    "of action. This action has therefore not been recorded against the user.");
                return;
            }

            var actionTaken = await _userSvc.AddActionTakenByTwitchUsernameAsync(userUsername, discordMod,
                (ActionTakenType)actionTakenType, duration, reason);

            await _userSvc.SendActionMessageAsync(actionTaken);
        }
    }
}
