namespace VainBotDiscord.Classes
{
    public class TwitterToCheck
    {
        public int Id { get; set; }

        public string TwitterUsername { get; set; }

        public long TwitterId { get; set; }

        public bool IncludeRetweets { get; set; }

        public long DiscordGuildId { get; set; }

        public long DiscordChannelId { get; set; }
    }
}
