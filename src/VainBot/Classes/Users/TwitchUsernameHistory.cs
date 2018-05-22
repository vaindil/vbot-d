using System;

namespace VainBot.Classes.Users
{
    public class TwitchUsernameHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public string Username { get; set; }

        public User User { get; set; }
    }
}
