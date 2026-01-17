namespace AzureP33.Models.Cosmos
{
    public class Product
    {
        public Guid id { get; set; }
        public Guid categoryId { get; set; }
        public string categoryName { get; set; } = null!;
        public string sku { get; set; } = null!;
        public string name { get; set; } = null!;
        public string description { get; set; } = null!;
        public double price { get; set; }
    }
}
