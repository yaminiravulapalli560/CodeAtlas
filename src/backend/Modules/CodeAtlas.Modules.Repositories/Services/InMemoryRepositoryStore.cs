namespace CodeAtlas.Modules.Repositories.Services;

using System.Collections.Concurrent;
using CodeAtlas.Modules.Repositories.Models;

/// <summary>
/// Thread-safe in-memory store for repository metadata.
/// Note: In-memory store loses state upon application restart.
/// </summary>
public sealed class InMemoryRepositoryStore : IRepositoryStore
{
    private readonly ConcurrentDictionary<Guid, Repository> _repositories = new();

    public Task<IReadOnlyList<Repository>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Repository> list = _repositories.Values
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _repositories.TryGetValue(id, out var repo);
        return Task.FromResult(repo);
    }

    public Task<Repository?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var repo = _repositories.Values.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(repo);
    }

    public Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        _repositories[repository.Id] = repository;
        return Task.FromResult(repository);
    }
}

