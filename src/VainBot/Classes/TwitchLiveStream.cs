using System;

namespace VainBot.Classes
{
    public class TwitchLiveStream
    {
        public string TwitchUserId { get; set; }

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset? FirstOfflineAt { get; set; }

        public string TwitchStreamId { get; set; }

        public string TwitchLogin { get; set; }

        public string TwitchDisplayName { get; set; }

        public int ViewerCount { get; set; }

        public string Title { get; set; }

        public string GameName { get; set; }

        public string GameId { get; set; }

        public string ThumbnailUrl { get; set; }

        public string ProfileImageUrl { get; set; }
    }
}
