using System.Net;
using System.Text.Json.Serialization;

namespace AzureP33.Models.ORM
{
    public class TranslatorResponseItem
    {
        [JsonPropertyName("translations")]
        public List<Translation> Translations { get; set; } = new();
    }
}
