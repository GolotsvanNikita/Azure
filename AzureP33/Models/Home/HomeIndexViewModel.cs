namespace AzureP33.Models.Home
{
    public class HomeIndexViewModel
    {
        public String PageTitle { get; set; } = "";

        public HomeIndexFormModel? FormModel { get; set; }

        public String? ErrorMessage { get; set; }

        //public static implicit operator HomeIndexViewModel?(HomeIndexFormModel? v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
