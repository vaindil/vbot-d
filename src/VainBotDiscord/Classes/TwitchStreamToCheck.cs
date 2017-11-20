namespace VainBotDiscord.Classes
{
    public class TwitchStreamToCheck
    {
        public int Id { get; set; }

        public string TwitchId { get; set; }

        public string Username { get; set; }

        public string MessageToPost { get; set; }

        public ulong ChannelId { get; set; }

        public ulong GuildId { get; set; }

        public bool IsEmbedded { get; set; }

        public bool IsDeleted { get; set; }

        public ulong? CurrentMessageId { get; set; }
    }
}
