﻿using Discord;
using Discord.Commands;
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
    public class UserModule : ModuleBase
    {
        readonly UserService _svc;

        const string _dtFormat = "yy-MM-dd hh:mm UTC";

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
                "`!addaction user <type> <duration> <reason (optional)>`: Adds an action to the given user. The duration should be in seconds. " +
                "A duration of -1 should be used for permanent actions. Example: `!addaction vaindil ban -1 he sucks`\n" +
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

        async Task<bool> ValidateNoteAsync(string note)
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

        [Command("notes")]
        [Alias("note")]
        public async Task GetNotesByDiscordUser(IUser discordUser)
        {
            var user = await _svc.GetOrCreateUserByDiscordAsync(discordUser);
            if (user.Notes?.Count == 0)
                await ReplyAsync($"No notes found for {discordUser.Username}.");
            else
                await GetNotesAsync(discordUser.Username, user.Notes);
        }

        [Command("notes")]
        [Alias("note")]
        public async Task GetNotesByTwitchUsername(string username)
        {
            var user = await _svc.GetOrCreateUserByTwitchUsernameAsync(username);
            if (user.Notes?.Count == 0)
                await ReplyAsync($"No notes found for {username}.");
            else
                await GetNotesAsync(username, user.Notes);
        }

        async Task GetNotesAsync(string name, List<UserNote> notes)
        {
            var sb = new StringBuilder("__Notes for ");
            sb.Append(name);
            sb.Append("__\n");

            foreach (var note in notes.OrderByDescending(x => x.LoggedAt))
            {
                sb.Append('[');
                sb.Append(note.LoggedAt.ToString(_dtFormat));
                sb.Append("] ");
                sb.Append(_svc.GetModName(note.ModeratorId));
                sb.Append(": ");
                sb.Append(note.Note);
                sb.Append("\n");
            }

            var msg = sb.ToString().TrimEnd('\n');
            if (msg.Length > 1800)
            {
                await ReplyAsync("This person's notes are way too long and vaindil hasn't written the code to handle this yet. " +
                    "Go yell at him.");
                return;
            }

            await ReplyAsync(msg);
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

            var user = await _svc.GetOrCreateUserByTwitchUsernameAsync(username);
            if (user.Aliases.Any(x => string.Equals(x.Alias, alias, System.StringComparison.OrdinalIgnoreCase)))
            {
                await ReplyAsync("User already has that alias.");
                return;
            }

            await _svc.AddAliasByTwitchUsernameAsync(username, Context.Message.Author, alias);
            await ReplyAsync("Alias added.");
        }

        async Task<bool> ValidateAliasAsync(string alias)
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

        async Task GetAliasesForUserAsync(string name, List<UserAlias> aliases)
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

        [Command("addaction")]
        public async Task AddActionByDiscordUser(IUser discordUser, string type, string durationStr, [Remainder]string reason = null)
        {
            var validResponse = await ValidateActionAsync(type, durationStr, reason);
            if (!validResponse.Item1.HasValue || !validResponse.Item2.HasValue)
                return;

            await _svc.AddActionTakenByDiscordIdAsync(discordUser, Context.Message.Author,
                validResponse.Item1.Value, validResponse.Item2.Value, reason);
            await ReplyAsync("Action added successfully.");
        }

        [Command("addaction")]
        public async Task AddActionByTwitchUsername(string twitchUsername, string type, string durationStr, [Remainder]string reason = null)
        {
            var validResponse = await ValidateActionAsync(type, durationStr, reason);
            if (!validResponse.Item1.HasValue || !validResponse.Item2.HasValue)
                return;

            await _svc.AddActionTakenByTwitchUsernameAsync(twitchUsername, Context.Message.Author,
                validResponse.Item1.Value, validResponse.Item2.Value, reason);
            await ReplyAsync("Action added successfully.");
        }

        async Task<(ActionTakenType?, int?)> ValidateActionAsync(string type, string durationStr, string reason)
        {
            if (!Enum.TryParse(typeof(ActionTakenType), type, true, out var actionTypeOut))
            {
                var validTypes = "";
                foreach (ActionTakenType t in Enum.GetValues(typeof(ActionTakenType)))
                {
                    validTypes += "`" + t.ToString().ToLower() + "`, ";
                }

                validTypes = validTypes.TrimEnd(',', ' ');

                await ReplyAsync("Action type is not valid. Valid types are " + validTypes + ".");
                return (null, null);
            }

            var actionType = (ActionTakenType)actionTypeOut;
            if (actionType == ActionTakenType.Warning)
                durationStr = "-1";

            if (!int.TryParse(durationStr, out var duration))
            {
                await ReplyAsync("Duration is not a valid integer.");
                return (null, null);
            }

            if (duration < -1 || duration == 0)
            {
                await ReplyAsync("Duration is invalid. Must be a positive number, or -1 for permanent.");
                return (null, null);
            }

            if (!string.IsNullOrWhiteSpace(reason) && reason.Length > 500)
            {
                await ReplyAsync("Reason must be 500 characters or fewer. Your reason was {reason.Length} characters long.");
                return (null, null);
            }

            return ((ActionTakenType)actionType, duration);
        }

        [Command("history")]
        public async Task GetHistoryByDiscordUser(IUser discordUser)
        {
            var user = await _svc.GetOrCreateUserByDiscordAsync(discordUser);
            await GetHistoryForUserAsync(discordUser.Username, user.ActionsAgainst);
        }

        [Command("history")]
        public async Task GetHistoryByTwitchUsername(string username)
        {
            var user = await _svc.GetOrCreateUserByTwitchUsernameAsync(username);
            await GetHistoryForUserAsync(username, user.ActionsAgainst);
        }

        async Task GetHistoryForUserAsync(string name, List<ActionTaken> actions)
        {
            if (actions.Count == 0)
            {
                await ReplyAsync($"{name} has not had any actions taken against them.");
                return;
            }

            var sb = new StringBuilder("__Actions taken against ");
            sb.Append(name);
            sb.Append("__\n");

            foreach (var action in actions)
            {
                sb.Append('[');
                sb.Append(action.LoggedAt.ToString(_dtFormat));
                sb.Append("] [");
                sb.Append(action.ActionTakenType.ToString().ToLower());
                sb.Append("] ");
                sb.Append(_svc.GetModName(action.ModeratorId));

                if (!string.IsNullOrEmpty(action.Reason))
                {
                    sb.Append(" | ");
                    sb.Append(action.Reason);
                }

                sb.Append("\n");
            }

            var msg = sb.ToString().TrimEnd('\n');

            if (msg.Length > 1800)
            {
                await ReplyAsync($"{name} is so bad that their actions combined generate a message that's too long for Discord. " +
                    "Yell at vaindil for not implementing this yet.");
                return;
            }

            await ReplyAsync(msg);
        }

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
    }
}