namespace CodeAtlas.Modules.Repositories.Services;

using CodeAtlas.Modules.Repositories.Models;

public interface IRepositoryStore
{
    Task<IReadOnlyList<Repository>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Repository?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default);
}

