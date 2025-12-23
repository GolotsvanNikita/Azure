using System.Text.Json.Serialization;

namespace AzureP33.Models.ORM
{
    public class TranslatorErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorData Error { get; set; } = null!;
    }
}
