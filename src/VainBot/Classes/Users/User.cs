using System.Collections.Generic;

namespace VainBot.Classes.Users
{
    public class User
    {
        public int Id { get; set; }

        public long TwitchId { get; set; }

        public long DiscordId { get; set; }

        public bool IsModerator { get; set; }

        public List<UserAlias> Aliases { get; set; }

        public List<TwitchUsernameHistory> TwitchUsernames { get; set; }

        public List<DiscordUsernameHistory> DiscordUsernames { get; set; }

        public List<ActionTaken> ActionsAgainst { get; set; }

        public List<UserNote> Notes { get; set; }

        public List<UserAlias> ModeratedAliases { get; set; }

        public List<ActionTaken> ModeratedActions { get; set; }

        public List<UserNote> ModeratedNotes { get; set; }
    }
}
