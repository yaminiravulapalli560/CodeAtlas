namespace CodeAtlas.Modules.Repositories.Models;

public sealed record Repository(
    Guid Id,
    string Name,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    string SourceType,
    string? RepositoryUrl);

