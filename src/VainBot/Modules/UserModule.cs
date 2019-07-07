using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VainBot.Classes.Users;
using VainBot.Preconditions;
using VainBot.Services;

namespace VainBot.Modules
{
    [FitzyGuild]
    [FitzyModChannel]
    [FitzyModerator]
    public class UserModule : InteractiveBase
    {
        private readonly UserService _svc;

        private const string _dtFormat = "yy-MM-dd hh:mm UTC";

        public UserModule(UserService svc)
        {
            _svc = svc;
        }

        [Command("user")]
        [Alias("users")]
        public async Task Help()
        {
            await ReplyAsync("The following commands are used to manage tracked users. The bot is able to parse " +
                "Discord users in a variety of ways, including a standard mention, by their username/discriminator in plain " +
                "text (such as `vaindil#4444`), or by just their nickname if it is unique on the server. Twitch usernames " +
                "must be their current username. In the commands below, the parameter `user` can refer to either of these two; " +
                "you do not need to specify which is being used.\n" +
                "\n" +
                "A user will be created in the system the first time that a command is used on them.\n" +
                "\n" +
                "`!link discord_user twitch_username`: Links the user's Discord account to their Twitch account.\n" +
                "`!addalias user alias`: Adds the provided alias to the user.\n" +
                "`!addnote user note`: Adds the provided note to the user.\n" +
                "`!addaction twitch` or `!addaction discord`: Starts an interactive game with the bot to add an action to a user on the " +
                "specified platform.\n" +
                "`!aliases user`: Gets the aliases associated with the user.\n" +
                "`!notes user`: Gets the notes associated with the user.\n" +
                "`!history user`: Gets the history of actions taken against this user.\n");
        }

        [Command("link")]
        public async Task LinkAccounts(IUser discordUser, string twitchUsername)
        {
            await _svc.LinkAccountsAsync(discordUser, twitchUsername);

            await ReplyAsync($"{discordUser.Username} is now linked to {twitchUsername}.");
        }

        [Command("addnote")]
        public async Task AddNoteByDiscordUser(IUser user, [Remainder]string note)
        {
            if (user == null)
            {
                await ReplyAsync("User must be specified.");
                return;
            }

            if (!await ValidateNoteAsync(note))
                return;

            await _svc.AddNoteByDiscordAsync(user, Context.Message.Author, note);
            await ReplyAsync("Note added.");
        }

        [Command("addnote")]
        public async Task AddNoteByTwitchUsername(string username, [Remainder]string note)
        {
            if (!await ValidateNoteAsync(note))
                return;

            await _svc.AddNoteByTwitchUsernameAsync(username, Context.Message.Author, note);
            await ReplyAsync("Note added.");
        }

        private async Task<bool> ValidateNoteAsync(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                await ReplyAsync("Note must be specified.");
                return false;
            }

            if (note.Length > 200)
            {
                await ReplyAsync("Note must be 200 characters or fewer.");
                return false;
            }

            return true;
        }

        [Command("history")]
        [Alias("notes", "note", "stalk")]
        public async Task GetNotesByDiscordUser(IUser discordUser)
        {
            var user = await _svc.GetOrCreateUserByDiscordAsync(discordUser);
            await GetActionsAsync(discordUser.Username, user);
        }

        [Command("history")]
        [Alias("notes", "note", "stalk")]
        public async Task GetNotesByTwitchUsername(string username, int page = 1)
        {
            if (page < 1)
                page = 1;

            if (username.Contains(' '))
            {
                await ReplyAsync("Invalid username provided.");
                return;
            }

            var user = await _svc.GetOrCreateUserByTwitchUsernameAsync(username);
            await GetActionsAsync(username, user, page);
        }

        private async Task GetActionsAsync(string name, User user, int page = 1)
        {
            var combined = new List<DetailsWrapper>();

            combined.AddRange(user.Notes.Select(x => new DetailsWrapper
            {
                LoggedAt = x.LoggedAt,
                Type = "Note",
                ModeratorId = x.ModeratorId,
                Content = x.Note
            }));

            combined.AddRange(user.ActionsAgainst.Select(x => new DetailsWrapper
            {
                LoggedAt = x.LoggedAt,
                Type = x.DurationSeconds > 0 ? $"{x.DurationSeconds}-second {x.ActionTakenType}".ToLower() : x.ActionTakenType.ToString(),
                ModeratorId = x.ModeratorId,
                Content = x.Reason
            }));

            var embed = new EmbedBuilder()
                .WithColor(new Color(0, 0, 255));

            string message = null;

            if (combined.Count > 25)
            {
                var totalPages = (int)Math.Ceiling(combined.Count / 25M);
                if (page > totalPages)
                    page = 1;

                message = $"**Page {page} of {totalPages}**";
            }

            if (user.TwitchUsernames.Count > 0)
            {
                embed.WithTitle($"Details for {name} - click to stalk")
                    .WithUrl($"https://ttv.overrustlelogs.net/Fitzyhere/{user.TwitchUsernames.Last().Username}");
            }
            else
            {
                embed.WithTitle($"Details for {name}");
            }

            if (combined.Count == 0)
            {
                embed.WithDescription($"{name} has no notes or actions taken against them.");
            }
            else
            {
                foreach (var i in combined.OrderByDescending(x => x.LoggedAt).Skip((page - 1) * 25).Take(25))
                {
                    embed.AddField($"{i.LoggedAt.ToString(_dtFormat)} | {_svc.GetModName(i.ModeratorId)}: {i.Type}", i.Content);
                }
            }

            await ReplyAsync(message, embed: embed.Build());
        }

        [Command("addalias")]
        public async Task AddAliasByDiscord(IUser discordUser, [Remainder]string alias)
        {
            if (!await ValidateAliasAsync(alias))
                return;

            var user = await _svc.GetOrCreateUserByDiscordAsync(discordUser);
            if (user.Aliases.Any(x => string.Equals(x.Alias, alias, System.StringComparison.OrdinalIgnoreCase)))
            {
                await ReplyAsync("User already has that alias.");
                return;
            }

            await _svc.AddAliasByDiscordAsync(discordUser, Context.Message.Author, alias);
            await ReplyAsync("Alias added.");
        }

        [Command("addalias")]
        public async Task AddAliasByTwitchUsername(string username, [Remainder]string alias)
        {
            if (!await ValidateAliasAsync(alias))
                return;

            if (username.Contains(' '))
            {
                await ReplyAsync("Invalid username provided.");
                return;
            }

            var user = await _svc.GetOrCreateUserByTwitchUsernameAsync(username);
            if (user.Aliases.Any(x => string.Equals(x.Alias, alias, System.StringComparison.OrdinalIgnoreCase)))
            {
                await ReplyAsync("User already has that alias.");
                return;
            }

            await _svc.AddAliasByTwitchUsernameAsync(username, Context.Message.Author, alias);
            await ReplyAsync("Alias added.");
        }

        private async Task<bool> ValidateAliasAsync(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                await ReplyAsync("Alias must be provided.");
                return false;
            }

            if (alias.Length > 100)
            {
                await ReplyAsync("Alias must be 100 characters or fewer (and even that's being generous).");
                return false;
            }

            return true;
        }

        [Command("alias")]
        [Alias("aliases")]
        public async Task GetAliasesByDiscordUser(IUser discordUser)
        {
            var user = await _svc.GetOrCreateUserByDiscordAsync(discordUser);
            await GetAliasesForUserAsync(discordUser.Username, user.Aliases);
        }

        [Command("alias")]
        [Alias("aliases")]
        public async Task GetAliasesByTwitchUsername(string username)
        {
            var user = await _svc.GetOrCreateUserByTwitchUsernameAsync(username);
            await GetAliasesForUserAsync(username, user.Aliases);
        }

        private async Task GetAliasesForUserAsync(string name, List<UserAlias> aliases)
        {
            if (aliases.Count == 0)
            {
                await ReplyAsync($"{name} has no aliases.");
                return;
            }

            var sb = new StringBuilder("__Aliases for ");
            sb.Append(name);
            sb.Append("__\n");

            foreach (var alias in aliases.OrderBy(x => x.Alias))
            {
                sb.Append(alias.Alias);
                sb.Append("\n");
            }

            var msg = sb.ToString().TrimEnd('\n');
            if (msg.Length > 1800)
            {
                await ReplyAsync($"{name}'s aliases are somehow longer than Discord will allow to fit in " +
                    "a single message. You're all ridiculous.");
                return;
            }

            await ReplyAsync(msg);
        }

        [Command("addaction discord", RunMode = RunMode.Async)]
        public async Task AddDiscordActionInteractive()
        {
            if (Context.Channel.Id != 432328775598866434)
            {
                await ReplyAsync("This command can only be used in <#432328775598866434>. Quit trying to spam!");
                return;
            }

            await ReplyAsync("Who was the naughty child? (for example, `vaindil#4444`)");

            var response = await NextMessageAsync();
            var parts = response.Content.Split('#', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                await ReplyAsync("Invalid response: That's not in the proper format of `vaindil#4444`. CANCELED!");
                return;
            }

            await Context.Client.DownloadUsersAsync(new[] { Context.Guild });

            var user = Context.Client.GetUser(parts[0], parts[1]);
            if (user == null)
            {
                await ReplyAsync("That user does not exist. CANCELED!");
                return;
            }

            await ReplyAsync($"Okay, action against {user.Mention}. What action was taken? Valid options are {GetValidActionTypes()}.");
            response = await NextMessageAsync();

            if (!Enum.TryParse(typeof(ActionTakenType), response.Content, true, out var typeOut))
            {
                await ReplyAsync("That's not a valid action type. CANCELED!");
                return;
            }

            var actionTakenType = (ActionTakenType)typeOut;

            await ReplyAsync($"So far so good. You did this: {actionTakenType} against {user.Mention}. Does this have a duration? " +
                "If so, how many seconds? You can use 0 for things with no duration, or -1 for things that are permanent.");

            response = await NextMessageAsync();

            if (!int.TryParse(response.Content, out var duration) || duration < -1)
            {
                await ReplyAsync("That's not a valid integer. CANCELED!");
                return;
            }

            var reply = $"Perfect, a duration of {duration} seconds.";
            if (duration == -1)
                reply = "Perfect, the action is permanent.";
            else if (duration == 0)
                reply = "Perfect, the action has no duration.";

            await ReplyAsync($"{reply} Last step! What's the reason for this action?");

            response = await NextMessageAsync();

            var action = await _svc.AddActionTakenByDiscordIdAsync(user, Context.Message.Author, actionTakenType, duration, response.Content);

            await ReplyAsync("Action added successfully. Thanks for playing!");

            await _svc.SendActionMessageAsync(action);
        }

        [Command("addaction twitch", RunMode = RunMode.Async)]
        public async Task AddTwitchActionInteractive()
        {
            if (Context.Channel.Id != 432328775598866434)
            {
                await ReplyAsync("This command can only be used in <#432328775598866434>. Quit trying to spam!");
                return;
            }

            await ReplyAsync("Who was the naughty child? (for example, `vaindil`)");

            var response = await NextMessageAsync();

            var user = response.Content;

            await ReplyAsync($"Okay, action against {user}. What action was taken? Valid options are {GetValidActionTypes()}.");
            response = await NextMessageAsync();

            if (!Enum.TryParse(typeof(ActionTakenType), response.Content, true, out var typeOut))
            {
                await ReplyAsync("That's not a valid action type. CANCELED!");
                return;
            }

            var actionTakenType = (ActionTakenType)typeOut;

            await ReplyAsync($"So far so good. You did this: {actionTakenType} against {user}. Does this have a duration? " +
                "If so, how many seconds? You can use 0 for things with no duration, or -1 for things that are permanent.");

            response = await NextMessageAsync();

            if (!int.TryParse(response.Content, out var duration) || duration < -1)
            {
                await ReplyAsync("That's not a valid integer. CANCELED!");
                return;
            }

            var reply = $"Perfect, a duration of {duration} seconds.";
            if (duration == -1)
                reply = "Perfect, the action is permanent.";
            else if (duration == 0)
                reply = "Perfect, the action has no duration.";

            await ReplyAsync($"{reply} Last step! What's the reason for this action?");

            response = await NextMessageAsync();

            var action = await _svc.AddActionTakenByTwitchUsernameAsync(user, Context.Message.Author, actionTakenType, duration, response.Content);

            await ReplyAsync("Action added successfully. Thanks for playing!");

            await _svc.SendActionMessageAsync(action, user);
        }

        [Command("reason", RunMode = RunMode.Async)]
        public async Task EditReason(int id, [Remainder]string reason)
        {
            if (await _svc.UpdateReasonAsync(id, reason))
                await ReplyAndDeleteAsync("Reason updated successfully.", timeout: TimeSpan.FromSeconds(5));
            else
                await ReplyAndDeleteAsync("Reason updated successfully.", timeout: TimeSpan.FromSeconds(5));

            await Task.Delay(5000);
            await Context.Message.DeleteAsync();
        }

        [Command("delete", RunMode = RunMode.Async)]
        [Alias("remove")]
        public async Task DeleteAction(int id)
        {
            if (await _svc.DeleteActionAsync(id, Context.User))
            {
                await ReplyAndDeleteAsync("Action deleted successfully.");

                await Task.Delay(5000);
                await Context.Message.DeleteAsync();
            }
            else
            {
                await ReplyAsync("Error occurred while deleting action. Yell at vaindil.");
            }
        }

        //[Command("addaction")]
        //public async Task AddActionByDiscordUser(IUser discordUser, string type, string durationStr, [Remainder]string reason = null)
        //{
        //    var validResponse = await ValidateActionAsync(type, durationStr, reason);
        //    if (!validResponse.Item1.HasValue || !validResponse.Item2.HasValue)
        //        return;

        //    await _svc.AddActionTakenByDiscordIdAsync(discordUser, Context.Message.Author,
        //        validResponse.Item1.Value, validResponse.Item2.Value, reason);
        //    await ReplyAsync("Action added successfully.");
        //}

        //[Command("addaction")]
        //public async Task AddActionByTwitchUsername(string twitchUsername, string type, string durationStr, [Remainder]string reason = null)
        //{
        //    var validResponse = await ValidateActionAsync(type, durationStr, reason);
        //    if (!validResponse.Item1.HasValue || !validResponse.Item2.HasValue)
        //        return;

        //    await _svc.AddActionTakenByTwitchUsernameAsync(twitchUsername, Context.Message.Author,
        //        validResponse.Item1.Value, validResponse.Item2.Value, reason);
        //    await ReplyAsync("Action added successfully.");
        //}

        private string GetValidActionTypes()
        {
            var validTypes = "";
            foreach (ActionTakenType t in Enum.GetValues(typeof(ActionTakenType)))
            {
                validTypes += "`" + t.ToString().ToLower() + "`, ";
            }

            return validTypes.TrimEnd(',', ' ');
        }

        //async Task<(ActionTakenType?, int?)> ValidateActionAsync(string type, string durationStr, string reason)
        //{
        //    if (!Enum.TryParse(typeof(ActionTakenType), type, true, out var actionTypeOut))
        //    {
        //        var validTypes = "";
        //        foreach (ActionTakenType t in Enum.GetValues(typeof(ActionTakenType)))
        //        {
        //            validTypes += "`" + t.ToString().ToLower() + "`, ";
        //        }

        //        validTypes = validTypes.TrimEnd(',', ' ');

        //        await ReplyAsync("Action type is not valid. Valid types are " + validTypes + ".");
        //        return (null, null);
        //    }

        //    var actionType = (ActionTakenType)actionTypeOut;
        //    if (actionType == ActionTakenType.Warning)
        //        durationStr = "-1";

        //    if (!int.TryParse(durationStr, out var duration))
        //    {
        //        await ReplyAsync("Duration is not a valid integer.");
        //        return (null, null);
        //    }

        //    if (duration < -1 || duration == 0)
        //    {
        //        await ReplyAsync("Duration is invalid. Must be a positive number, or -1 for permanent.");
        //        return (null, null);
        //    }

        //    if (!string.IsNullOrWhiteSpace(reason) && reason.Length > 500)
        //    {
        //        await ReplyAsync("Reason must be 500 characters or fewer. Your reason was {reason.Length} characters long.");
        //        return (null, null);
        //    }

        //    return ((ActionTakenType)actionType, duration);
        //}

        [Command("mod")]
        [FitzyAdmin]
        public async Task ToggleMod(IUser discordUser)
        {
            var user = await _svc.GetOrCreateUserByDiscordAsync(discordUser);
            if (user.Aliases.Count == 0)
            {
                await ReplyAsync("User must have an alias before you can mark them as a mod.");
                return;
            }

            var isAlreadyMod = user.IsModerator;

            await _svc.ToggleModAsync(discordUser);

            if (isAlreadyMod)
                await ReplyAsync($"{discordUser.Username} is no longer marked as a mod.");
            else
                await ReplyAsync($"{discordUser.Username} is now marked as a mod.");
        }

        private class DetailsWrapper
        {
            public DateTimeOffset LoggedAt { get; set; }
            public int ModeratorId { get; set; }
            public string Type { get; set; }
            public string Content { get; set; }
        }

        private enum ActionLocation
        {
            Discord,
            Twitch
        }
    }
}
