using System.Text.Json.Serialization;

namespace AzureP33.Models.ORM
{
    public class Translation
    {
        [JsonPropertyName("text")]
        public String Text { get; set; } = null!;

        [JsonPropertyName("to")]
        public String ToLang { get; set; } = null!;
    }
}
