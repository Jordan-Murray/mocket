using Microsoft.Azure.Cosmos;
using Container = Microsoft.Azure.Cosmos.Container;
using User = Mocket.Models.User;

namespace Mocket.Services
{
    public interface ICosmosDbService
    {
        Task AddItemAsync<T>(T item);
        Task<T> GetItemAsync<T>(string id);
        Task<IEnumerable<T>> GetItemsAsync<T>(string query);
        Task UpdateItemAsync<T>(string id, T item);
        Task DeleteItemAsync<T>(string id);

        Task AddUserAsync(User user);
        Task<User?> GetUserAsync(string username);
    }

    public class CosmosDbService(CosmosClient dbClient, string databaseName, string containerName) : ICosmosDbService
    {
        private Container _container = dbClient.GetContainer(databaseName, containerName);

        public async Task AddItemAsync<T>(T item)
        {
            await _container.CreateItemAsync(item, new PartitionKey(((dynamic)item).Id));
        }

        public async Task<T> GetItemAsync<T>(string id)
        {
            try
            {
                ItemResponse<T> response = await _container.ReadItemAsync<T>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException)
            {
                return default;
            }
        }

        public async Task<IEnumerable<T>> GetItemsAsync<T>(string query)
        {
            var queryIterator = _container.GetItemQueryIterator<T>(new QueryDefinition(query));
            List<T> results = new List<T>();
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task UpdateItemAsync<T>(string id, T item)
        {
            await _container.UpsertItemAsync(item, new PartitionKey(id));
        }

        public async Task DeleteItemAsync<T>(string id)
        {
            await _container.DeleteItemAsync<T>(id, new PartitionKey(id));
        }

        public async Task AddUserAsync(User user)
        {
            await _container.CreateItemAsync(user, new PartitionKey(user.Id));
        }

        public async Task<User?> GetUserAsync(string username)
        {
            var query = $"SELECT * FROM c WHERE c.username = '{username}'";
            var result = await GetItemsAsync<User>(query);
            return result.FirstOrDefault();
        }
    }
}
