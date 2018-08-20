using System;

namespace VainBot.Classes.Users
{
    public class ActionTaken
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ModeratorId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public ActionTakenType ActionTakenType { get; set; }

        public int DurationSeconds { get; set; }

        public string Reason { get; set; }

        public long? DiscordMessageId { get; set; }

        public string Source { get; set; }

        public User User { get; set; }

        public User Moderator { get; set; }
    }

    public enum ActionTakenType
    {
        Warning,
        Timeout,
        Ban,
        TemporaryBan,
        Unban,
        Untimeout
    }
}
