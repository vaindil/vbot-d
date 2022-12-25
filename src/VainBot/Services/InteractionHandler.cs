using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace VainBot.Services
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InteractionHandler> _logger;

        public InteractionHandler(
            DiscordSocketClient client,
            InteractionService interactionService,
            IServiceProvider services,
            IConfiguration config,
            ILogger<InteractionHandler> logger)
        {
            _client = client;
            _interactionService = interactionService;
            _services = services;
            _configuration = config;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _client.Ready += ReadyAsync;

            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.InteractionCreated += HandleInteraction;
        }

        private async Task ReadyAsync()
        {
            if (Program.IsDebug())
            {
                await _interactionService.RegisterCommandsToGuildAsync(_configuration.GetValue<ulong>("test_guild_id"), true);
                _logger.LogInformation("debug mode, registered guild commands");
            }
            else
            {
                await _interactionService.RegisterCommandsGloballyAsync(true);
                _logger.LogInformation("release mode, registered global commands");
            }
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);

                var result = await _interactionService.ExecuteCommandAsync(context, _services);

                if (!result.IsSuccess)
                {
                    var msg = "An unknown error occurred while running that command.";

                    switch (result.Error)
                    {
                        case InteractionCommandError.ConvertFailed:
                            msg = "You used the wrong type of parameter somewhere in there. Please try again.";
                            break;

                        case InteractionCommandError.BadArgs:
                            msg = "You provided the wrong number of arguments.";
                            break;

                        case InteractionCommandError.UnknownCommand:
                            msg = "I don't know how to handle that command.";
                            break;

                        case InteractionCommandError.UnmetPrecondition:
                            msg = "You can't use that command.";
                            break;

                        default:
                            _logger.LogError($"[{context.Guild.Name}][{context.Channel.Name}][{context.User.Username}] " +
                                $"Command error: {result.ErrorReason}");
                            break;
                    }

                    await interaction.RespondAsync($"{context.User.Mention}: {msg}");
                }
            }
            catch
            {
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}
