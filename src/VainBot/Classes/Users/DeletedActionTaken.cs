using System;

namespace VainBot.Classes.Users
{
    public class DeletedActionTaken
    {
        public DeletedActionTaken() { }

        public DeletedActionTaken(ActionTaken action, int moderatorId, DateTimeOffset deletedAt)
        {
            DeletedById = moderatorId;
            DeletedAt = deletedAt;

            UserId = action.UserId;
            ModeratorId = action.ModeratorId;
            LoggedAt = action.LoggedAt;
            ActionTakenType = action.ActionTakenType;
            DurationSeconds = action.DurationSeconds;
            Reason = action.Reason;
            DiscordMessageId = action.DiscordMessageId;
            Source = action.Source;
        }

        public int Id { get; set; }

        public int UserId { get; set; }

        public int ModeratorId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public ActionTakenType ActionTakenType { get; set; }

        public int DurationSeconds { get; set; }

        public string Reason { get; set; }

        public long? DiscordMessageId { get; set; }

        public string Source { get; set; }

        public int DeletedById { get; set; }

        public DateTimeOffset DeletedAt { get; set; }
    }
}
