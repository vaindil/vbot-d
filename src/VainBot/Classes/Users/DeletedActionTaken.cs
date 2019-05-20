using System;

namespace VainBot.Classes.Users
{
    public class DeletedActionTaken : ActionTaken
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

        public int DeletedById { get; set; }

        public DateTimeOffset DeletedAt { get; set; }
    }
}
