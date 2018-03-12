using System;

namespace VainBot.Classes
{
    public class YouTubeChannelToCheck
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public long DiscordGuildId { get; set; }

        public long DiscordChannelId { get; set; }

        public string DiscordMessageToPost { get; set; }

        public string YouTubeChannelId { get; set; }

        public string YouTubePlaylistId { get; set; }

        public bool IsDeleted { get; set; }

        public string LatestVideoId { get; set; }

        public DateTime? LatestVideoUploadedAt { get; set; }

        public long? DiscordMessageId { get; set; }
    }
}
