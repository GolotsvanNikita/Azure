namespace AzureP33.Services.CosmosDB
{
    public static class CosmosDbServiceExtension
    {
        public static void AddCosmosDb(this IServiceCollection services) 
        {
            services.AddSingleton<ICosmosDbService, CosmosDbService>();
        }
    }
}
