namespace BusinessLayer.Services
{
    public interface ISecurityCounterStore
    {
        Task<long> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}