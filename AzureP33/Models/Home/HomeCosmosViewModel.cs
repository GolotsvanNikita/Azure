using AzureP33.Models.Cosmos;

namespace AzureP33.Models.Home
{
    public class HomeCosmosViewModel
    {
        public List<Product> Products { get; set; } = new();
        public double RequestCharge { get; set; }
        public List<Product> AvailableCategories { get; set; } = new();
        public string? SelectedCategoryId { get; set; }
    }
}
