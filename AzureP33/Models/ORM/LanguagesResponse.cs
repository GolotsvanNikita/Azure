using Microsoft.AspNetCore.Authentication;
using System.Text.Json.Serialization;

namespace AzureP33.Models.ORM
{
    public class LanguagesResponse
    {
        [JsonPropertyName("translation")]
        public Dictionary<String, LangData> Transltations { get; set; } = new();
    }
}
