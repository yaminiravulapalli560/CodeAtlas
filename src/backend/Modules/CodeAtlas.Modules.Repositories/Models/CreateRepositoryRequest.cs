namespace CodeAtlas.Modules.Repositories.Models;

public sealed record CreateRepositoryRequest(
    string? Name,
    string? Description,
    string? SourceType,
    string? RepositoryUrl);

