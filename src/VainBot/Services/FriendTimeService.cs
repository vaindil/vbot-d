using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VainBot.Classes.FriendTime;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class FriendTimeService
    {
        private readonly ILogger<FriendTimeService> _logger;
        private readonly IServiceProvider _provider;

        public FriendTimeService(ILogger<FriendTimeService> logger, IServiceProvider provider)
        {
            _logger = logger;
            _provider = provider;
        }

        public async Task AddOrUpdateFriendTimeAsync(ulong guildId, ulong channelId, ulong userId, string timezoneId)
        {
            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                var existing = await db.FriendTimes
                    .FirstOrDefaultAsync(f => f.GuildId == (long)guildId && f.ChannelId == (long)channelId && f.UserId == (long)userId);

                if (existing != null)
                {
                    existing.Timezone = timezoneId;
                    db.FriendTimes.Update(existing);
                }
                else
                {
                    var newFt = new FriendTime
                    {
                        GuildId = (long)guildId,
                        ChannelId = (long)channelId,
                        UserId = (long)userId,
                        Timezone = timezoneId
                    };
                    db.FriendTimes.Add(newFt);
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in friend time service: add/update friend time");
                throw;
            }
        }

        public async Task<bool> RemoveFriendTimeAsync(ulong guildId, ulong channelId, ulong userId)
        {
            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                var existing = await db.FriendTimes
                    .FirstOrDefaultAsync(f => f.GuildId == (long)guildId && f.ChannelId == (long)channelId && f.UserId == (long)userId);

                if (existing != null)
                {
                    db.FriendTimes.Remove(existing);
                    await db.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error updating database in friend time service: remove friend time");
                throw;
            }
        }

        public async Task<List<FriendTime>> GetFriendTimesForChannelAsync(ulong guildId, ulong channelId)
        {
            try
            {
                using var db = _provider.GetRequiredService<VbContext>();

                return await db.FriendTimes
                    .Where(f => f.GuildId == (long)guildId && f.ChannelId == (long)channelId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error querying database in friend time service: get friend times for channel");
                return new List<FriendTime>();
            }
        }
    }
}
