namespace BusinessLayer.Services
{
    public class CacheSettings
    {
        public string Provider { get; set; } = "Memory"; // Memory | Redis
        public string RedisConnection { get; set; } = string.Empty;

        public bool UseRedis => string.Equals(Provider, "Redis", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(RedisConnection);
    }
}