namespace VainBotDiscord.Classes
{
    public class TwitterToCheck
    {
        public int Id { get; set; }

        public string TwitterUsername { get; set; }

        public long TwitterId { get; set; }

        public bool IncludeRetweets { get; set; }

        public ulong DiscordGuildId { get; set; }

        public ulong DiscordChannelId { get; set; }
    }
}
