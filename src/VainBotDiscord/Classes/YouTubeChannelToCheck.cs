using System;

namespace VainBotDiscord.Classes
{
    public class YouTubeChannelToCheck
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public ulong DiscordGuildId { get; set; }

        public ulong DiscordChannelId { get; set; }

        public string DiscordMessageToPost { get; set; }

        public string YouTubeChannelId { get; set; }

        public string YouTubePlaylistId { get; set; }

        public bool IsDeleted { get; set; }

        public string LatestVideoId { get; set; }

        public DateTime? LatestVideoUploadedAt { get; set; }

        public ulong? DiscordMessageId { get; set; }
    }
}
