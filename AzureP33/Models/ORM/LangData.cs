using AzureP33.Models.Home;
using System.Text.Json.Serialization;

namespace AzureP33.Models.ORM
{
    public class LangData
    {
        [JsonPropertyName("name")]
        public String Name { get; set; } = null!;

        [JsonPropertyName("nativeName")]
        public String NativeName { get; set; } = null!;

        [JsonPropertyName("dir")]
        public String? Direction { get; set; } = null;

        [JsonPropertyName("scripts")]
        public LangData[]? Scripts { get; set; } = null!;

        [JsonPropertyName("toScripts")]
        public LangData[]? ToScripts { get; set; } = null!;

        [JsonPropertyName("code")]
        public String? Code { get; set; } = null!;

        public HomeIndexFormModel? Form { get; set; }
    }
}
