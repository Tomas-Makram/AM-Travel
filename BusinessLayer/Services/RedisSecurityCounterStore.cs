using StackExchange.Redis;

namespace BusinessLayer.Services
{
    public sealed class RedisSecurityCounterStore : ISecurityCounterStore
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisSecurityCounterStore(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<long> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();

            long value = await db.StringIncrementAsync(key);

            if (value == 1)
            {
                await db.KeyExpireAsync(key, window);
            }

            return value;
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
    }
}