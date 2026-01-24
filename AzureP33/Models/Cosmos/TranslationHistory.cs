using AzureP33.Models.ORM;
using Newtonsoft.Json;

namespace AzureP33.Models.Cosmos
{
    public class TranslationHistory
    {
        [JsonProperty("id")]
        public string id { get; set; } = Guid.NewGuid().ToString();

        public string? UserId { get; set; }

        public string OriginalText { get; set; } = null!;
        public string TranslatedText { get; set; } = null!;

        public string FromLang { get; set; } = null!;
        public string ToLang { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TransliterationResponseItem? FromTransliteration { get; set; }
        public TransliterationResponseItem? ToTransliteration { get; set; }

        public string Type { get; set; } = "Translation";

        [JsonProperty("category")]
        public string Category { get; set; } = "History";
    }
}
