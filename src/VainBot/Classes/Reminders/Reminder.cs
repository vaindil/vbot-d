using System;

namespace VainBot.Classes.Reminders
{
    public class Reminder
    {
        public int Id { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset FireAt { get; set; }

        public long UserId { get; set; }

        public long ChannelId { get; set; }

        public long RequestingMessageId { get; set; }

        public long? GuildId { get; set; }

        public string Message { get; set; }

        /// <summary>
        /// If false, this reminder has already fired and is no longer active. Records are kept in case
        /// the user snoozes the reminder.
        /// alter table reminder add column is_active boolean not null default true;
        /// </summary>
        public bool IsActive { get; set; }
    }
}
