using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VainBot.Classes.Users;
using VainBot.Infrastructure;

namespace VainBot.Services
{
    public class UserService
    {
        readonly IServiceProvider _provider;
        readonly DiscordSocketClient _discord;
        readonly TwitchService _twitchSvc;

        List<Mod> _mods;

        public UserService(IServiceProvider provider)
        {
            _provider = provider;
            _discord = _provider.GetRequiredService<DiscordSocketClient>();
            _twitchSvc = _provider.GetRequiredService<TwitchService>();

            _mods = new List<Mod>();
        }

        public async Task InitializeAsync()
        {
            using (var db = Db())
            {
                _mods = await db.Users
                    .Include(x => x.Aliases)
                    .Where(x => x.IsModerator)
                    .Select(x => new Mod(x.Id, x.DiscordId.Value, x.Aliases[0].Alias))
                    .ToListAsync();
            }
        }

        public async Task<User> GetOrCreateUserByDiscordAsync(IUser user)
        {
            using (var db = Db())
            {
                var userId = (long)user.Id;
                var dbUser = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == userId)
                    ?? await CreateUserFromDiscordAsync(user);

                return await GetUserByIdAsync(dbUser.Id);
            }
        }

        public async Task<User> GetOrCreateUserByTwitchUsernameAsync(string username)
        {
            username = username.ToLowerInvariant();

            using (var db = Db())
            {
                User user;

                var dbUsername = await db.TwitchUsernameHistories.FirstOrDefaultAsync(x => x.Username == username);
                if (dbUsername != null)
                    user = await GetUserByIdAsync(dbUsername.UserId);
                else
                {
                    user = await CreateUserFromTwitchAsync(username);
                    user = await GetUserByIdAsync(user.Id);
                }

                return user;
            }
        }

        async Task<User> GetUserByIdAsync(int id)
        {
            using (var db = Db())
            {
                return await db.Users
                    .Include(x => x.Aliases)
                    .Include(x => x.ActionsAgainst)
                    .Include(x => x.DiscordUsernames)
                    .Include(x => x.TwitchUsernames)
                    .Include(x => x.Notes)
                    .Include(x => x.ModeratedActions)
                    .Include(x => x.ModeratedAliases)
                    .Include(x => x.ModeratedNotes)
                    .FirstOrDefaultAsync(x => x.Id == id);
            }
        }

        async Task<User> CreateUserFromDiscordAsync(IUser user)
        {
            var dbUser = new User
            {
                DiscordId = (long)user.Id,
                IsModerator = false
            };

            using (var db = Db())
            {
                db.Users.Add(dbUser);
                await db.SaveChangesAsync();

                db.DiscordUsernameHistories.Add(new DiscordUsernameHistory
                {
                    UserId = dbUser.Id,
                    LoggedAt = DateTimeOffset.UtcNow,
                    Username = user.Username,
                    Discriminator = user.Discriminator
                });

                await db.SaveChangesAsync();
            }

            return dbUser;
        }

        async Task<User> CreateUserFromTwitchAsync(string username, string twitchId = null)
        {
            username = username.ToLowerInvariant();

            if (twitchId == null)
            {
                var (id, displayName) = await _twitchSvc.GetUserIdAsync(username);
                twitchId = id;
            }

            var dbUser = new User
            {
                TwitchId = twitchId,
                IsModerator = false
            };

            using (var db = Db())
            {
                db.Users.Add(dbUser);
                await db.SaveChangesAsync();

                db.TwitchUsernameHistories.Add(new TwitchUsernameHistory
                {
                    UserId = dbUser.Id,
                    LoggedAt = DateTimeOffset.UtcNow,
                    Username = username
                });

                await db.SaveChangesAsync();
            }

            return dbUser;
        }

        public async Task LinkAccountsAsync(IUser discordUser, string twitchUsername)
        {
            twitchUsername = twitchUsername.ToLowerInvariant();

            using (var db = Db())
            {
                var dbTwitchUser = await GetOrCreateUserByTwitchUsernameAsync(twitchUsername);
                var dbDiscordUser = await GetOrCreateUserByDiscordAsync(discordUser);

                foreach (var a in dbDiscordUser.Aliases)
                    a.UserId = dbTwitchUser.Id;

                foreach (var u in dbDiscordUser.TwitchUsernames)
                    u.UserId = dbTwitchUser.Id;

                foreach (var u in dbDiscordUser.DiscordUsernames)
                    u.UserId = dbTwitchUser.Id;

                foreach (var aa in dbDiscordUser.ActionsAgainst)
                    aa.UserId = dbTwitchUser.Id;

                foreach (var n in dbDiscordUser.Notes)
                    n.UserId = dbTwitchUser.Id;

                foreach (var ma in dbDiscordUser.ModeratedAliases)
                    ma.ModeratorId = dbTwitchUser.Id;

                foreach (var ma in dbDiscordUser.ModeratedActions)
                    ma.ModeratorId = dbTwitchUser.Id;

                foreach (var mn in dbDiscordUser.ModeratedNotes)
                    mn.ModeratorId = dbTwitchUser.Id;

                await db.SaveChangesAsync();

                db.Users.Remove(dbDiscordUser);
                await db.SaveChangesAsync();
            }
        }

        public async Task AddNoteByDiscordAsync(IUser discordUser, IUser moderator, string note)
        {
            var user = await GetOrCreateUserByDiscordAsync(discordUser);
            await AddNoteByIdAsync(user.Id, GetModId(moderator), note);
        }

        public async Task AddNoteByTwitchUsernameAsync(string twitchUsername, IUser moderator, string note)
        {
            var user = await GetOrCreateUserByTwitchUsernameAsync(twitchUsername);
            await AddNoteByIdAsync(user.Id, GetModId(moderator), note);
        }

        async Task AddNoteByIdAsync(int userId, int moderatorId, string note)
        {
            using (var db = Db())
            {
                db.UserNotes.Add(new UserNote
                {
                    UserId = userId,
                    ModeratorId = moderatorId,
                    LoggedAt = DateTimeOffset.UtcNow,
                    Note = note
                });

                await db.SaveChangesAsync();
            }
        }

        public async Task AddAliasByDiscordAsync(IUser discordUser, IUser moderator, string alias)
        {
            var user = await GetOrCreateUserByDiscordAsync(discordUser);
            await AddAliasByIdAsync(user.Id, GetModId(moderator), alias);
        }

        public async Task AddAliasByTwitchUsernameAsync(string twitchUsername, IUser moderator, string alias)
        {
            var user = await GetOrCreateUserByTwitchUsernameAsync(twitchUsername);
            await AddAliasByIdAsync(user.Id, GetModId(moderator), alias);
        }

        async Task AddAliasByIdAsync(int userId, int moderatorId, string alias)
        {
            using (var db = Db())
            {
                db.UserAliases.Add(new UserAlias
                {
                    UserId = userId,
                    ModeratorId = moderatorId,
                    AddedAt = DateTimeOffset.UtcNow,
                    Alias = alias
                });

                await db.SaveChangesAsync();
            }
        }

        public async Task AddActionTakenByDiscordIdAsync(
            IUser discordUser, IUser moderator, ActionTakenType type, int duration, string reason = null)
        {
            var user = await GetOrCreateUserByDiscordAsync(discordUser);
            await AddActionTakenByIdAsync(user.Id, GetModId(moderator), type, duration, reason);
        }

        public async Task AddActionTakenByTwitchUsernameAsync(
            string twitchUsername, IUser moderator, ActionTakenType type, int duration, string reason = null)
        {
            var user = await GetOrCreateUserByTwitchUsernameAsync(twitchUsername);
            await AddActionTakenByIdAsync(user.Id, GetModId(moderator), type, duration, reason);
        }

        async Task AddActionTakenByIdAsync(int userId, int moderatorId, ActionTakenType type, int duration, string reason = null)
        {
            using (var db = Db())
            {
                db.ActionsTaken.Add(new ActionTaken
                {
                    UserId = userId,
                    ModeratorId = moderatorId,
                    LoggedAt = DateTimeOffset.UtcNow,
                    ActionTakenType = type,
                    DurationSeconds = duration,
                    Reason = reason
                });

                await db.SaveChangesAsync();
            }
        }

        public async Task ToggleModAsync(IUser discordUser)
        {
            var mod = _mods.Find(x => x.DiscordId == discordUser.Id);
            if (mod != null)
            {
                _mods.Remove(mod);
                using (var db = Db())
                {
                    var discordUserId = (long)discordUser.Id;
                    var dbUser = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == discordUserId);
                    if (dbUser == null)
                        return;

                    dbUser.IsModerator = false;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                using (var db = Db())
                {
                    var discordUserId = (long)discordUser.Id;
                    var dbUser = await db.Users.FirstOrDefaultAsync(x => x.DiscordId == discordUserId)
                        ?? await CreateUserFromDiscordAsync(discordUser);

                    dbUser.IsModerator = true;
                    db.Users.Update(dbUser);

                    await db.SaveChangesAsync();

                    await db.Entry(dbUser).Collection(x => x.Aliases).LoadAsync();

                    _mods.Add(new Mod(dbUser.Id, discordUser.Id, dbUser.Aliases[0].Alias));
                }
            }
        }

        public bool CheckIfUserIsMod(ulong discordId)
        {
            return _mods.Any(x => x.DiscordId == discordId);
        }

        public string GetModName(int id)
        {
            return _mods.Find(x => x.Id == id)?.Name;
        }

        int GetModId(IUser discordUser)
        {
            return _mods.First(x => x.DiscordId == discordUser.Id).Id;
        }

        VbContext Db()
        {
            return _provider.GetRequiredService<VbContext>();
        }

        class Mod
        {
            public Mod(int id, ulong discordId, string name)
            {
                Id = id;
                DiscordId = discordId;
                Name = name;
            }

            public Mod(int id, long discordId, string name)
            {
                Id = id;
                DiscordId = (ulong)discordId;
                Name = name;
            }

            public int Id { get; set; }

            public ulong DiscordId { get; set; }

            public string Name { get; set; }
        }
    }
}
