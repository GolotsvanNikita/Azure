using Microsoft.Azure.Cosmos;

namespace AzureP33.Services.CosmosDB
{
    public interface ICosmosDbService
    {
        Task<Container> GetContainerAsync();

    }
}
