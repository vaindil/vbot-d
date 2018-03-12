using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace VainBot.Classes
{
    public class TwitchTokenResponse
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresInSeconds { get; set; }

        // scope and refresh token aren't needed
    }

    public class TwitchErrorResponse
    {
        public string Error { get; set; }

        public int Status { get; set; }

        public string Message { get; set; }
    }

    public class TwitchUserResponse
    {
        [JsonProperty(PropertyName = "data")]
        public List<TwitchUser> Users { get; set; }
    }

    public class TwitchStreamResponse
    {
        [JsonProperty(PropertyName = "data")]
        public List<TwitchStream> Streams { get; set; }
    }

    public class TwitchGameResponse
    {
        [JsonProperty(PropertyName = "data")]
        public List<TwitchGame> Games { get; set; }
    }

    public class TwitchUser
    {
        public string Id { get; set; }

        public string Login { get; set; }

        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        public string Type { get; set; }

        [JsonProperty(PropertyName = "broadcaster_type")]
        public string BroadcasterType { get; set; }

        public string Description { get; set; }

        [JsonProperty(PropertyName = "profile_image_url")]
        public string ProfileImageUrl { get; set; }

        [JsonProperty(PropertyName = "offline_image_url")]
        public string OfflineImageUrl { get; set; }

        [JsonProperty(PropertyName = "view_count")]
        public int ViewCount { get; set; }
    }

    public class TwitchStream
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "user_id")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "game_id")]
        public string GameId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public TwitchStreamType Type { get; set; }

        public string Title { get; set; }

        [JsonProperty(PropertyName = "viewer_count")]
        public int ViewerCount { get; set; }

        [JsonProperty(PropertyName = "started_at")]
        public DateTimeOffset StartedAt { get; set; }

        public string Language { get; set; }

        [JsonProperty(PropertyName = "thumbnail_url")]
        public string ThumbnailUrl { get; set; }
    }

    public class TwitchGame
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonProperty(PropertyName = "box_art_url")]
        public string BoxArtUrl { get; set; }
    }

    public enum TwitchStreamType
    {
        Live,
        Vodcast
    }
}
