using System.Text.Json.Serialization;

namespace AzureP33.Models.ORM
{
    public class ErrorData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public String Message { get; set; } = null!;
    }
}
