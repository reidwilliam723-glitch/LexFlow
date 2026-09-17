namespace Lexon.Core.Interfaces;

/// <summary>
/// Interface for profile management
/// </summary>
public interface IProfile
{
    string Id { get; }
    string Name { get; }
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task LoadAsync(CancellationToken cancellationToken = default);
    bool HasSetting(string key);
}
