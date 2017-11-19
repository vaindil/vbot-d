using Newtonsoft.Json;
using System.Collections.Generic;

namespace VainBotDiscord.Classes
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
        public List<TwitchUser> Data { get; set; }
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
}
