namespace LexFlow.Core.Interfaces;

/// <summary>
/// Interface for encrypted storage operations
/// </summary>
public interface IStorage
{
    Task SaveAsync<T>(string key, T data, CancellationToken cancellationToken = default);
    Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
