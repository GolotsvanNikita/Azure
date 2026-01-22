using AzureP33.Models.ORM;
using Newtonsoft.Json;

namespace AzureP33.Models.Cosmos
{
    public class TranslationHistory
    {
        public static readonly string PartitionKey = "History";

        [JsonProperty("id")]
        public Guid Id { get; set; }
        public string? userId { get; set; }

        public string OriginalText { get; set; }
        public string TranslatedText { get; set; }
        public string FromLang { get; set; }
        public string ToLang { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("category")]
        public string Category { get; set; } = TranslationHistory.PartitionKey;

        public TransliterationResponseItem? FromTransliteration { get; set; }
        public TransliterationResponseItem? ToTransliteration { get; set; }

        public string? FromTranResult = null!;
        public string? ToTranResult = null!;

        public string Type { get; set; } = "Translation";
    }
}
