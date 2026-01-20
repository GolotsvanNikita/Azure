using Microsoft.Azure.Cosmos;

namespace AzureP33.Services.CosmosDB
{
    public class CosmosDbService : ICosmosDbService
    {
        private Container? container;
        private readonly IConfiguration _configuration;

        public CosmosDbService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<Container> GetContainerAsync()
        {
            if (container == null) 
            {
                var sec = _configuration.GetSection("Azure").GetSection("CosmosDB") ?? throw new Exception("Configuration error: Azure.CosmosDB is null");

                if (sec == null) throw new Exception("Configuration error");

                string connectionString = sec.GetValue<string>("ConnectionString") ?? throw new Exception("Configuration error: 'ConnectionString' is null");
                string databaseId = sec.GetValue<string>("DatabaseId") ?? throw new Exception("Configuration error: 'DatabaseId' is null");
                string containerId = sec.GetValue<string>("ContainerId") ?? throw new Exception("Configuration error: 'ContainerId' is null");

                CosmosClient client = new(connectionString: connectionString);

                Database database = await client.GetDatabase(databaseId).ReadAsync();

                container = await database.GetContainer(containerId).ReadContainerAsync();
            }
            return container!;
        }
    }
}
