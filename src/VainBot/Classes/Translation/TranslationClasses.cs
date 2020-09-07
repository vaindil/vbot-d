using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VainBot.Classes.Translation
{
    // file mostly copied from the link below, not all are used
    // https://docs.microsoft.com/en-us/azure/cognitive-services/Translator/quickstart-translate?pivots=programming-language-csharp

    public class TranslationResult
    {
        [JsonPropertyName("detectedLanguage")]
        public DetectedLanguage DetectedLanguage { get; set; }

        [JsonPropertyName("sourceText")]
        public TextResult SourceText { get; set; }

        [JsonPropertyName("translations")]
        public List<Translation> Translations { get; set; }
    }

    public class DetectedLanguage
    {
        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("score")]
        public float Score { get; set; }
    }

    public class TextResult
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("script")]
        public string Script { get; set; }
    }

    public class Translation
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("transliteration")]
        public TextResult Transliteration { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("alignment")]
        public Alignment Alignment { get; set; }

        [JsonPropertyName("sentLen")]
        public SentenceLength SentLen { get; set; }
    }

    public class Alignment
    {
        [JsonPropertyName("proj")]
        public string Proj { get; set; }
    }

    public class SentenceLength
    {
        [JsonPropertyName("srcSentLen")]
        public List<int> SrcSentLen { get; set; }

        [JsonPropertyName("transSentLen")]
        public List<int> TransSentLen { get; set; }
    }

    public class TranslationErrorWrapper
    {
        [JsonPropertyName("error")]
        public TranslationError Error { get; set; }
    }

    public class TranslationError
    {
        [JsonPropertyName("code")]
        public long Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }
}
