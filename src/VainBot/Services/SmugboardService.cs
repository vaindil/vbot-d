using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VainBot.Classes.Smugboard;
using VainBot.Configs;
using VainBot.Infrastructure;

namespace VainBot.Services;

public class SmugboardService
{
    private readonly DiscordSocketClient _discord;
    private readonly ILogger<SmugboardService> _logger;
    private readonly IServiceProvider _provider;
    private readonly SmugboardConfig _config;

    private readonly List<ulong> _savedMessageIds = [];

    public SmugboardService(
        DiscordSocketClient discord,
        ILogger<SmugboardService> logger,
        IServiceProvider provider,
        IOptions<SmugboardConfig> options)
    {
        _discord = discord;
        _logger = logger;
        _provider = provider;
        _config = options.Value;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing smugboard service");
        List<SmugboardMessage> messages;
        
        try
        {
            await using var db = _provider.GetRequiredService<VbContext>();
            messages = await db.SmugboardMessages.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Smugboard service could not be initialized");
            return;
        }

        foreach (var m in messages)
        {
            _savedMessageIds.Add(m.MessageId);
        }

        _discord.ReactionAdded += ReactionAddedAsync;
    }

    private async Task ReactionAddedAsync(
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> cachedChannel,
        SocketReaction reaction)
    {
        var message = await cachedMessage.GetOrDownloadAsync();
        var channel = await cachedChannel.GetOrDownloadAsync();
        var overThreshold = message.Reactions.Values.Where(x => x.ReactionCount >= 1);
        if (!overThreshold.Any() || _savedMessageIds.Contains(message.Id))
            return;

        if (!await IsInCategoryAsync(channel))
            return;
        
        await PostMessageAsync(message, (ITextChannel)channel);
        await SavePostToDbAsync(message.Id);
    }

    private async Task<bool> IsInCategoryAsync(IMessageChannel channel)
    {
        switch (channel.GetChannelType())
        {
            case ChannelType.Text:
                return ((INestedChannel)channel).CategoryId == _config.InsideCategoryId;

            case ChannelType.NewsThread:
            case ChannelType.PublicThread:
            case ChannelType.PrivateThread:
                var mainId = ((IThreadChannel)channel).CategoryId;
                if (!mainId.HasValue)
                    return false;

                var mainChannel = await _discord.GetChannelAsync(mainId.Value);
                return ((INestedChannel)mainChannel).CategoryId == _config.InsideCategoryId;
            default:
                return false;
        }
    }

    private async Task PostMessageAsync(IMessage message, ITextChannel origChannel)
    {
        var postInChannel = (IMessageChannel)await _discord.GetChannelAsync(_config.PostInChannelId);
        var messageUrl = $"https://discordapp.com/channels/{origChannel.Guild.Id}/{origChannel.Id}/{message.Id}";
        var description = message.Content + $"\n\n\u2192 [original message]({messageUrl}) in {origChannel.Mention}";

        var embed = new EmbedBuilder()
            .WithDescription(description)
            .WithAuthor(message.Author)
            .Build();

        await postInChannel.SendMessageAsync(embed: embed);
        _savedMessageIds.Add(message.Id);
    }

    private async Task SavePostToDbAsync(ulong messageId)
    {
        try
        {
            await using var db = _provider.GetRequiredService<VbContext>();

            db.SmugboardMessages.Add(new SmugboardMessage(messageId));
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Error updating database during with smugboard message ID {messageId}");
        }
    }
}