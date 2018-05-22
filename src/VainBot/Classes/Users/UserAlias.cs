using System;

namespace VainBot.Classes.Users
{
    public class UserAlias
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ModeratorId { get; set; }

        public DateTimeOffset AddedAt { get; set; }

        public string Alias { get; set; }

        public User User { get; set; }

        public User Moderator { get; set; }
    }
}
