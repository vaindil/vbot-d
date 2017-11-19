using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using VainBotDiscord.Classes;
using VainBotDiscord.Services;

namespace VainBotDiscord.Modules
{
    public class TwitchIdModule : ModuleBase<SocketCommandContext>
    {
        readonly TwitchService _twitchSvc;
        readonly HttpClient _httpClient;

        public TwitchIdModule(TwitchService twitchSvc, HttpClient httpClient)
        {
            _twitchSvc = twitchSvc;
            _httpClient = httpClient;
        }

        [Command("twitchid")]
        public async Task GetId(string username)
        {
            var request = _twitchSvc.GetRequestMessage();
            request.RequestUri = new Uri($"https://api.twitch.tv/helix/users?login={username}");
            request.Method = HttpMethod.Get;

            var response = await _httpClient.SendAsync(request);
            try
            {
                await _twitchSvc.ThrowIfResponseInvalidAsync(response);
            }
            catch
            {
                await ReplyAsync("An error occurred when getting the ID. Please try again, or yell at vaindil.");
                return;
            }

            var users = JsonConvert.DeserializeObject<TwitchUserResponse>(await response.Content.ReadAsStringAsync());
            if (users.Data.Count == 0)
            {
                await ReplyAsync($"The user **{username}** does not exist.");
                return;
            }

            await ReplyAsync($"**{users.Data[0].DisplayName}**: {users.Data[0].Id}");
        }
    }
}
