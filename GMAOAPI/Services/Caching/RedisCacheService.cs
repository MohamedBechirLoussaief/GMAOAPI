using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace GMAOAPI.Services.Caching
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisCacheService(
            IDistributedCache cache,
            IConnectionMultiplexer connectionMultiplexer)
        {
            _cache = cache;
            _connectionMultiplexer = connectionMultiplexer;
        }

        public T? GetData<T>(string key)
        {
            var data = _cache.GetString(key);
            if (data == null) return default;
            return JsonSerializer.Deserialize<T>(data);
        }

        public void SetData<T>(string key, T data)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            _cache.SetString(key, JsonSerializer.Serialize(data), options);
        }

        public void RemoveData(string key)
        {
            _cache.Remove(key);
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            var endpoints = _connectionMultiplexer.GetEndPoints();
            var server = _connectionMultiplexer.GetServer(endpoints.First());

            var keys = server.Keys(pattern: prefix + "*");

            var db = _connectionMultiplexer.GetDatabase();
            await Task.WhenAll(keys.Select(key => db.KeyDeleteAsync(key)));
        }
    }
}
