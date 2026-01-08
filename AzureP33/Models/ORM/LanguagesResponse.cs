using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Serialization;

namespace AzureP33.Models.ORM
{
    public class LanguagesResponse
    {
        [JsonPropertyName("translation")]
        public Dictionary<String, LangData> Transltations { get; set; } = new();

        [JsonPropertyName("transliteration")]
        public Dictionary<String, LangData> Translatirations { get; set; } = new();
    }
}
