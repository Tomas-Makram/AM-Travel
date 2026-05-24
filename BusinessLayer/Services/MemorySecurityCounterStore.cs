using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace BusinessLayer.Services
{
    public sealed class MemorySecurityCounterStore : ISecurityCounterStore
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, object> _locks = new();

        public MemorySecurityCounterStore(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Task<long> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
        {
            var gate = _locks.GetOrAdd(key, _ => new object());

            lock (gate)
            {
                if (!_memoryCache.TryGetValue<long>(key, out var current))
                {
                    current = 0;
                }

                current++;

                _memoryCache.Set(key, current, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = window
                });

                return Task.FromResult(current);
            }
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _memoryCache.Remove(key);
            return Task.CompletedTask;
        }
    }
}