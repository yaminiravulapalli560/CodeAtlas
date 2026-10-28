namespace CodeAtlas.Common.Models;

public sealed record SystemStatusResponse(
    string Status,
    string Service,
    string Environment,
    string Version,
    DateTimeOffset TimestampUtc,
    IReadOnlyCollection<string> ActiveModules);
