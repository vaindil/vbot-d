using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VainBot.Classes.Translation;
using VainBot.Configs;

namespace VainBot.Modules
{
    public class TranslateModule : ModuleBase
    {
        private readonly TranslationConfig _config;
        private readonly HttpClient _httpClient;

        private const string _langUrl = "<https://docs.microsoft.com/en-us/azure/cognitive-services/translator/language-support#text-translation>";

        public TranslateModule(IOptions<TranslationConfig> options, HttpClient httpClient)
        {
            _config = options.Value;
            _httpClient = httpClient;
        }

        [Command("translate help")]
        [Alias("translatefrom help", "tr help", "trfrom help")]
        public async Task Help([Remainder]string unused = null)
        {
            await ReplyAsync("Translate text from one language to another, automatically detecting the source language: " +
                "`![translate/tr] tolang Text to translate`\n" +
                "Translate text, specifying the source language: `![translatefrom/trfrom] fromlang tolang Text to translate`\n" +
                "Examples:\n" +
                "```\n" +
                "!translate de This text will be translated to German, with the source automatically detected.\n" +
                "!translatefrom en de This text will be translated to German, forcing the source language to be English.\n" +
                "!tr de Translate to German\n" +
                "!trfrom en de Translate to German, forcing English as source\n" +
                "```\n" +
                "Supported languages can be found here: " + _langUrl);
        }

        [Command("translate")]
        [Alias("tr")]
        public async Task Translate(string dest, [Remainder]string text)
        {
            if (!(await IsTextValidAsync(text)))
                return;

            await Context.Channel.TriggerTypingAsync();

            var (result, error) = await MakeApiCall(dest, text);
            if (error != null)
            {
                await ReplyAsync(error);
                return;
            }

            var embed = BuildEmbed(result, text);
            await ReplyAsync(embed: embed);
        }

        [Command("translatefrom")]
        [Alias("trfrom")]
        public async Task TranslateFrom(string source, string dest, [Remainder]string text)
        {
            if (!(await IsTextValidAsync(text)))
                return;

            await Context.Channel.TriggerTypingAsync();

            var (result, error) = await MakeApiCall(dest, text, source);
            if (error != null)
            {
                await ReplyAsync(error);
                return;
            }

            var embed = BuildEmbed(result, text, source);
            await ReplyAsync(embed: embed);
        }

        private async Task<bool> IsTextValidAsync(string text)
        {
            const int maxLength = 300;
            if (text.Length > maxLength)
            {
                await ReplyAsync($"Text to be translated must not be longer than {maxLength} characters.");
                return false;
            }

            return true;
        }

        private async Task<(TranslationResult Result, string Error)> MakeApiCall(string dest, string text, string source = null)
        {
            var url = $"{_config.ApiEndpoint}translate?api-version=3.0&to={dest}";
            if (!string.IsNullOrWhiteSpace(source))
                url += $"&from={source}";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(new[] { new { Text = text } }), Encoding.UTF8, "application/json"),
            };

            request.Headers.Add("Ocp-Apim-Subscription-Key", _config.ApiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _config.ApiRegion);

            using var response = await _httpClient.SendAsync(request);
            var responseStream = await response.Content.ReadAsStreamAsync();
            // var str = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var error = await JsonSerializer.DeserializeAsync<TranslationErrorWrapper>(responseStream);
                switch (error.Error.Code)
                {
                    case 400035:
                        return (null, $"The specified 'from' language is not valid. See {_langUrl} for valid languages.");

                    case 400036:
                        return (null, $"The specified 'to' language is not valid. See {_langUrl} for valid languages.");
                }

                Console.WriteLine($"Error from Azure translation API: {error.Error.Code} | {error.Error.Message}");
                return (null, "Unspecified error occurred when translating text. The bot's free quota for the month was probably reached. Sorry!");
            }

            var respBody = await JsonSerializer.DeserializeAsync<List<TranslationResult>>(responseStream);
            if (respBody.Count > 0)
                return (respBody[0], null);

            return (null, "error");
        }

        private Embed BuildEmbed(TranslationResult result, string sourceText, string sourceLang = null)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Translation Result")
                .WithColor(1779902)
                .WithFooter("Powered by Microsoft Azure Translator", "https://vaindil.dev/azure_favicon.png");

            if (result.DetectedLanguage != null)
            {
                var score = Math.Round(result.DetectedLanguage.Score * 100, 1);
                embedBuilder.AddField(
                    "Source Lang (Detected)",
                    $"{result.DetectedLanguage.Language} (confidence: {score}/100)",
                    true);
            }
            else if (!string.IsNullOrWhiteSpace(sourceLang))
            {
                embedBuilder.AddField("Source Lang", sourceLang, true);
            }

            embedBuilder.AddField("To Lang", result.Translations[0].To, true);
            embedBuilder.AddField("Original Text", sourceText);

            var translatedText = result.Translations[0].Text;
            if (string.IsNullOrWhiteSpace(translatedText))
                translatedText = "(no translation returned by Azure)";

            embedBuilder.AddField("Translated Text", translatedText);

            return embedBuilder.Build();
        }
    }
}
