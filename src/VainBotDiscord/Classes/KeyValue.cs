namespace VainBotDiscord.Classes
{
    public class KeyValue
    {
        public KeyValue() { }
        public KeyValue(string key, string val)
        {
            Key = key;
            Value = val;
        }

        public string Key { get; set; }

        public string Value { get; set; }
    }

    public static class KeyValueKeys
    {
        public const string TwitchAccessToken = "twitch_access_token";
        public const string TwitchAccessTokenJobId = "twitch_access_token_job_id";
    }
}
