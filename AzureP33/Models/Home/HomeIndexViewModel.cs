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

        public String? FromTransliteration { get; set; }
        public String? ToTransliteration { get; set; }

        public LangData? Lang { get; set; }
        public HomeIndexFormModel? Form { get; set; }

    }
}
