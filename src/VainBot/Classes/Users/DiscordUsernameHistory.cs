using System;

namespace VainBot.Classes.Users
{
    public class DiscordUsernameHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTimeOffset LoggedAt { get; set; }

        public string Username { get; set; }

        public short Discriminator { get; set; }

        public User User { get; set; }
    }
}
