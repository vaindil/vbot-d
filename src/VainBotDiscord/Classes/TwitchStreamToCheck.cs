namespace VainBotDiscord.Classes
{
    public class TwitchStreamToCheck
    {
        public int Id { get; set; }

        public string TwitchId { get; set; }

        public string Username { get; set; }

        public string MessageToPost { get; set; }

        public long ChannelId { get; set; }

        public long GuildId { get; set; }

        public bool IsEmbedded { get; set; }

        public bool IsDeleted { get; set; }

        public long? CurrentMessageId { get; set; }
    }
}
