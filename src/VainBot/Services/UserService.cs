using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _discord;
        private readonly DiscordRestClient _discordRest;
        private readonly TwitchService _twitchSvc;
        private readonly ILogger<UserService> _logger;

        private const ulong _actionsChannelId = 480178651837628436;

        private List<Mod> _mods;

        public UserService(IServiceProvider provider, ILogger<UserService> logger)
        {
            _provider = provider;
            _discord = _provider.GetRequiredService<DiscordSocketClient>();
            _discordRest = _provider.GetRequiredService<DiscordRestClient>();
            _twitchSvc = _provider.GetRequiredService<TwitchService>();
            _logger = logger;

            _mods = new List<Mod>();
        }

        public async Task InitializeAsync()
        {
            using (var db = Db())
            {
                _mods = await db.Users.AsQueryable()
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
                var dbUser = await db.Users.AsQueryable().FirstOrDefaultAsync(x => x.DiscordId == userId)
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

                var dbUsername = await db.TwitchUsernameHistories.AsQueryable().FirstOrDefaultAsync(x => x.Username == username);
                if (dbUsername != null)
                {
                    return await GetUserByIdAsync(dbUsername.UserId);
                }
                else
                {
                    user = await CreateUserFromTwitchAsync(username);
                    return await GetUserByIdAsync(user.Id);
                }
            }
        }

        private async Task<User> GetUserByIdAsync(int id)
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

        private async Task<User> CreateUserFromDiscordAsync(IUser user)
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

        private async Task<User> CreateUserFromTwitchAsync(string username, string twitchId = null)
        {
            username = username.ToLowerInvariant();

            if (twitchId == null)
            {
                var (id, _) = await _twitchSvc.GetUserIdAsync(username);
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

                if (dbTwitchUser.Id == dbDiscordUser.Id)
                    return;

                foreach (var a in dbDiscordUser.Aliases)
                {
                    db.UserAliases.Update(a);
                    a.UserId = dbTwitchUser.Id;
                }

                foreach (var u in dbDiscordUser.TwitchUsernames)
                {
                    db.TwitchUsernameHistories.Update(u);
                    u.UserId = dbTwitchUser.Id;
                }

                foreach (var u in dbDiscordUser.DiscordUsernames)
                {
                    db.DiscordUsernameHistories.Update(u);
                    u.UserId = dbTwitchUser.Id;
                }

                foreach (var aa in dbDiscordUser.ActionsAgainst)
                {
                    db.ActionsTaken.Update(aa);
                    aa.UserId = dbTwitchUser.Id;
                }

                foreach (var n in dbDiscordUser.Notes)
                {
                    db.UserNotes.Update(n);
                    n.UserId = dbTwitchUser.Id;
                }

                foreach (var ma in dbDiscordUser.ModeratedAliases)
                {
                    db.UserAliases.Update(ma);
                    ma.ModeratorId = dbTwitchUser.Id;
                }

                foreach (var ma in dbDiscordUser.ModeratedActions)
                {
                    db.ActionsTaken.Update(ma);
                    ma.ModeratorId = dbTwitchUser.Id;
                }

                foreach (var mn in dbDiscordUser.ModeratedNotes)
                {
                    db.UserNotes.Update(mn);
                    mn.ModeratorId = dbTwitchUser.Id;
                }

                dbTwitchUser.IsModerator = dbDiscordUser.IsModerator;
                dbTwitchUser.DiscordId = dbDiscordUser.DiscordId;

                db.Users.Update(dbTwitchUser);

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

        private async Task AddNoteByIdAsync(int userId, int moderatorId, string note)
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

        private async Task AddAliasByIdAsync(int userId, int moderatorId, string alias)
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

        public async Task<ActionTaken> AddActionTakenByDiscordIdAsync(
            IUser discordUser, IUser moderator, ActionTakenType type, int duration, string reason = null)
        {
            var user = await GetOrCreateUserByDiscordAsync(discordUser);
            return await AddActionTakenByIdAsync(user.Id, GetModId(moderator), type, duration, "Discord", reason);
        }

        public async Task<ActionTaken> AddActionTakenByTwitchUsernameAsync(
            string twitchUsername, IUser moderator, ActionTakenType type, int duration, string reason = null)
        {
            var user = await GetOrCreateUserByTwitchUsernameAsync(twitchUsername);
            return await AddActionTakenByIdAsync(user.Id, GetModId(moderator), type, duration, "Twitch", reason);
        }

        private async Task<ActionTaken> AddActionTakenByIdAsync(int userId, int moderatorId, ActionTakenType type,
            int duration, string source, string reason = null)
        {
            using (var db = Db())
            {
                var actionTaken = new ActionTaken
                {
                    UserId = userId,
                    ModeratorId = moderatorId,
                    LoggedAt = DateTimeOffset.UtcNow,
                    ActionTakenType = type,
                    DurationSeconds = duration,
                    Reason = reason,
                    Source = source
                };

                db.ActionsTaken.Add(actionTaken);
                await db.SaveChangesAsync();

                return actionTaken;
            }
        }

        public async Task<bool> UpdateReasonAsync(int actionId, string newReason)
        {
            ActionTaken action;

            using (var db = Db())
            {
                action = await db.ActionsTaken.FindAsync(actionId);
                if (action == null)
                    return false;

                action.Reason = newReason;

                db.ActionsTaken.Update(action);
                await db.SaveChangesAsync();
            }

            await SendActionMessageAsync(action);

            return true;
        }

        public async Task<bool> DeleteActionAsync(int actionId, IUser moderator)
        {
            var modId = GetModId(moderator);

            try
            {
                using (var db = Db())
                {
                    var action = await db.ActionsTaken.FindAsync(actionId);
                    if (action != null)
                    {
                        var deletedAction = new DeletedActionTaken(action, modId, DateTimeOffset.UtcNow);
                        db.DeletedActionsTaken.Add(deletedAction);

                        db.ActionsTaken.Remove(action);

                        await db.SaveChangesAsync();

                        if (deletedAction.DiscordMessageId.HasValue)
                        {
                            var actionsChannel = (SocketTextChannel)_discord.GetChannel(_actionsChannelId);
                            var msg = await actionsChannel.GetMessageAsync((ulong)deletedAction.DiscordMessageId.Value);

                            if (msg != null)
                            {
                                await msg.DeleteAsync();
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting action ID {actionId}");
                return false;
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
                    var dbUser = await db.Users.AsQueryable().FirstOrDefaultAsync(x => x.DiscordId == discordUserId);
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
                    var dbUser = await db.Users.AsQueryable().FirstOrDefaultAsync(x => x.DiscordId == discordUserId)
                        ?? await CreateUserFromDiscordAsync(discordUser);

                    dbUser.IsModerator = true;
                    db.Users.Update(dbUser);

                    await db.SaveChangesAsync();

                    await db.Entry(dbUser).Collection(x => x.Aliases).LoadAsync();

                    _mods.Add(new Mod(dbUser.Id, discordUser.Id, dbUser.Aliases[0].Alias));
                }
            }
        }

        public async Task<IUser> GetDiscordUserByTwitchUsername(string twitchUsername)
        {
            ulong userId = 0;

            using (var db = Db())
            {
                var twitch = await db.TwitchUsernameHistories.AsQueryable().FirstOrDefaultAsync(x => x.Username == twitchUsername);
                if (twitch == null)
                    return null;

                var user = await db.Users.AsQueryable().FirstOrDefaultAsync(x => x.Id == twitch.UserId);
                if (user?.DiscordId.HasValue != true)
                    return null;

                userId = (ulong)user.DiscordId.Value;
            }

            return _discord.GetUser(userId) ?? (IUser)await _discordRest.GetUserAsync(userId);
        }

        public async Task SendActionMessageAsync(ActionTaken action, string username = null, IUser discordMod = null)
        {
            ulong modId = 0;
            var actionCount = 0;

            if (username == null)
            {
                using var db = Db();

                modId = (ulong)(await db.Users.FindAsync(action.ModeratorId)).DiscordId.Value;

                var user = await db.Users
                    .Include(x => x.TwitchUsernames)
                    .FirstOrDefaultAsync(x => x.Id == action.UserId);
                if (user == null)
                    return;

                actionCount = await db.ActionsTaken.AsQueryable()
                    .CountAsync(x => x.UserId == user.Id && x.ActionTakenType != ActionTakenType.Unban && x.ActionTakenType != ActionTakenType.Untimeout);

                if (action.Source == "Twitch")
                    username = user.TwitchUsernames.OrderByDescending(x => x.LoggedAt).First().Username;
                else
                    username = (_discord.GetUser((ulong)user.DiscordId) ?? (IUser)await _discordRest.GetUserAsync((ulong)user.DiscordId)).Mention;
            }

            if (discordMod == null)
                discordMod = _discord.GetUser(modId) ?? (IUser)await _discordRest.GetUserAsync(modId);

            var durationString = "";
            if (action.DurationSeconds == -1)
                durationString = "Permanent";
            else if (action.DurationSeconds > 0)
                durationString = $"{action.DurationSeconds}-second";

            Color color;
            switch (action.ActionTakenType)
            {
                case ActionTakenType.Warning:
                    color = new Color(255, 242, 179);
                    break;

                case ActionTakenType.Timeout:
                    color = new Color(255, 128, 0);
                    break;

                case ActionTakenType.TemporaryBan:
                case ActionTakenType.Ban:
                    color = new Color(255, 0, 0);
                    break;

                case ActionTakenType.Untimeout:
                case ActionTakenType.Unban:
                    color = new Color(38, 230, 0);
                    break;

                default:
                    color = new Color(255, 255, 255);
                    break;
            }

            var actionString = action.ActionTakenType.ToString().ToLower();

            var embed = new EmbedBuilder()
                .WithColor(color)
                .WithTitle($"{action.Source} Action (click for logs)")
                .WithFooter(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"))
                .WithUrl($"https://www.twitch.tv/popout/fitzyhere/viewercard/{username}")
                .AddField("User", username, true)
                .AddField("Action", $"{durationString} {actionString}", true)
                .AddField("Reason", action.Reason, true)
                .AddField("Total Violations", actionCount, true)
                .AddField("Responsible Mod", discordMod.Mention, true)
                .AddField("Edit reason with", $"!reason {action.Id}", true)
                .Build();

            var actionChannel = _discord.GetChannel(480178651837628436) as SocketTextChannel;

            if (action.DiscordMessageId.HasValue)
            {
                var message = await actionChannel.GetMessageAsync((ulong)action.DiscordMessageId.Value) as RestUserMessage;
                await message.ModifyAsync(x =>
                {
                    x.Content = discordMod.Mention;
                    x.Embed = embed;
                });
            }
            else
            {
                var message = await actionChannel.SendMessageAsync(discordMod.Mention, embed: embed);

                using (var db = Db())
                {
                    action.DiscordMessageId = (long)message.Id;

                    db.ActionsTaken.Update(action);
                    await db.SaveChangesAsync();
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

        private int GetModId(IUser discordUser)
        {
            return _mods.First(x => x.DiscordId == discordUser.Id).Id;
        }

        private VbContext Db()
        {
            return _provider.GetRequiredService<VbContext>();
        }

        private class Mod
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
