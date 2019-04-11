using Newtonsoft.Json;
using System;

namespace VainBot.Classes.Twitch
{
    public class StreamChangedNotification
    {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "user_name")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "game_id")]
        public string GameId { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "viewer_count")]
        public int? ViewerCount { get; set; }

        [JsonProperty(PropertyName = "started_at")]
        public DateTimeOffset? StartedAt { get; set; }

        [JsonProperty(PropertyName = "thumbnail_url")]
        public string ThumbnailUrl { get; set; }
    }
}
