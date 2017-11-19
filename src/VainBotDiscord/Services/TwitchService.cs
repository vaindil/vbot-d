using Hangfire;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using VainBotDiscord.Classes;

namespace VainBotDiscord.Services
{
    public class TwitchService
    {
        readonly HttpClient _httpClient;
        readonly IConfiguration _config;
        IServiceProvider _provider;

        string _accessToken;
        string _accessTokenJobId;

        public TwitchService(HttpClient httpClient, IConfiguration config, IServiceProvider provider)
        {
            _httpClient = httpClient;
            _config = config;
            _provider = provider;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;

            using (var db = _provider.GetRequiredService<VbContext>())
            {
                _accessToken = await db.KeyValues.GetValueAsync(KeyValueKeys.TwitchAccessToken);
                _accessTokenJobId = await db.KeyValues.GetValueAsync(KeyValueKeys.TwitchAccessTokenJobId);
            }

            if (string.IsNullOrEmpty(_accessToken))
                await RefreshAccessTokenAsync();
        }

        /// <summary>
        /// Gets the current access token stored in memory.
        /// </summary>
        /// <returns>Twitch access token</returns>
        public string GetAccessToken()
        {
            return _accessToken;
        }

        /// <summary>
        /// Gets an HttpRequestMessage with credentials specified that can be used to query the Twitch API.
        /// </summary>
        /// <returns>HttpRequestMessage</returns>
        public HttpRequestMessage GetRequestMessage()
        {
            var request = new HttpRequestMessage();
            request.Headers.Add("Client-ID", _config["twitch_client_id"]);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");
            return request;
        }

        /// <summary>
        /// Refreshes the access token.
        /// </summary>
        public async Task RefreshAccessTokenAsync()
        {
            var url = QueryHelpers.AddQueryString(
                "https://api.twitch.tv/kraken/oauth2/token",
                new Dictionary<string, string>
                {
                    ["client_id"] = _config["twitch_client_id"],
                    ["client_secret"] = _config["twitch_client_secret"],
                    ["grant_type"] = "client_credentials"
                });

            var tokenResponse = await _httpClient.PostAsync(url, null);
            await ThrowIfResponseInvalidAsync(tokenResponse);

            var token = JsonConvert.DeserializeObject<TwitchTokenResponse>(await tokenResponse.Content.ReadAsStringAsync());
            _accessToken = token.AccessToken;

            // schedule the token refresh 7 days early
            var jobId = BackgroundJob.Schedule(() => RefreshAccessTokenAsync(), TimeSpan.FromSeconds(token.ExpiresInSeconds - 604800));

            using (var db = _provider.GetRequiredService<VbContext>())
            {
                var accessToken = await db.KeyValues.FindAsync(KeyValueKeys.TwitchAccessToken);
                accessToken.Value = _accessToken;

                var accessTokenJobId = await db.KeyValues.FindAsync(KeyValueKeys.TwitchAccessTokenJobId);
                if (!string.IsNullOrEmpty(accessTokenJobId.Value))
                    BackgroundJob.Delete(accessTokenJobId.Value);

                accessTokenJobId.Value = jobId;

                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Throws an InvalidOperationException for the provided response if
        /// the response was an error.
        /// </summary>
        /// <param name="response">HttpResponseMessage to check</param>
        public async Task ThrowIfResponseInvalidAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var body = await response.Content.ReadAsStringAsync();
            var error = JsonConvert.DeserializeObject<TwitchErrorResponse>(body);

            throw new InvalidOperationException(
                $"Twitch token refresh failed with error code {error.Status}, {error.Error}. " +
                $"Message: {error.Message}");
        }
    }
}
