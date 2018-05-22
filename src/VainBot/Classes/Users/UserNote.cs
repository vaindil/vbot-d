using System;

namespace VainBot.Classes.Users
{
    public class UserNote
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ModeratorId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public string Note { get; set; }

        public User User { get; set; }

        public User Moderator { get; set; }
    }
}
