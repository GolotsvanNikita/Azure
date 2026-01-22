using AzureP33.Models.Cosmos;
using AzureP33.Models.ORM;

namespace AzureP33.Models.Home
{
    public class HomeIndexViewModel
    {
        public String PageTitle { get; set; } = "";

        public HomeIndexFormModel? FormModel { get; set; }

        public String? ErrorMessage { get; set; }

        public LanguagesResponse LanguagesResponse { get; set; } = null!;
        public TranslatorErrorResponse? ErrorResponse { get; set; }
        public List<TranslatorResponseItem> Items { get; set; }

        public TransliterationResponseItem? FromTransliteration { get; set; }
        public TransliterationResponseItem? ToTransliteration { get; set; }

        public List<TranslationHistory> History { get; set; } = new();

        public LangData? Lang { get; set; }
        public HomeIndexFormModel? Form { get; set; }

    }
}
