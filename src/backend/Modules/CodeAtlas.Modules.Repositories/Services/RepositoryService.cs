namespace CodeAtlas.Modules.Repositories.Services;

using CodeAtlas.Modules.Repositories.Models;

public sealed record ValidationResult(bool IsValid, IDictionary<string, string[]> Errors)
{
    public static ValidationResult Success() => new(true, new Dictionary<string, string[]>());

    public static ValidationResult Failed(string field, string message) =>
        new(false, new Dictionary<string, string[]>
        {
            [field] = [message]
        });

    public static ValidationResult Failed(IDictionary<string, string[]> errors) =>
        new(false, errors);
}

public interface IRepositoryService
{
    Task<IReadOnlyList<Repository>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(Repository? Repository, ValidationResult Validation, bool IsConflict)> CreateAsync(CreateRepositoryRequest request, CancellationToken cancellationToken = default);
}

public sealed class RepositoryService(IRepositoryStore store) : IRepositoryService
{
    private static readonly HashSet<string> AllowedSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Local",
        "Remote"
    };

    public Task<IReadOnlyList<Repository>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return store.GetAllAsync(cancellationToken);
    }

    public Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return store.GetByIdAsync(id, cancellationToken);
    }

    public async Task<(Repository? Repository, ValidationResult Validation, bool IsConflict)> CreateAsync(
        CreateRepositoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, List<string>>();

        // Validate Name
        var trimmedName = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            AddError(errors, "Name", "Repository name is required.");
        }
        else if (trimmedName.Length > 100)
        {
            AddError(errors, "Name", "Repository name cannot exceed 100 characters.");
        }

        // Validate Description
        var trimmedDescription = request.Description?.Trim();
        if (trimmedDescription is not null && trimmedDescription.Length > 500)
        {
            AddError(errors, "Description", "Repository description cannot exceed 500 characters.");
        }

        // Validate SourceType
        var trimmedSourceType = request.SourceType?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedSourceType))
        {
            AddError(errors, "SourceType", "Source type is required. Supported values: 'Local', 'Remote'.");
        }
        else if (!AllowedSourceTypes.Contains(trimmedSourceType))
        {
            AddError(errors, "SourceType", "Invalid source type. Supported values are: 'Local', 'Remote'.");
        }
        else
        {
            // Normalize casing
            trimmedSourceType = trimmedSourceType.Equals("Local", StringComparison.OrdinalIgnoreCase) ? "Local" : "Remote";
        }

        // Validate RepositoryUrl
        var trimmedUrl = request.RepositoryUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(trimmedUrl))
        {
            if (trimmedUrl.Length > 2000)
            {
                AddError(errors, "RepositoryUrl", "Repository URL cannot exceed 2000 characters.");
            }
            else if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var parsedUri) ||
                     (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
            {
                AddError(errors, "RepositoryUrl", "Repository URL must be a valid absolute HTTP or HTTPS URL.");
            }
        }
        else
        {
            trimmedUrl = null;
        }

        if (errors.Count > 0)
        {
            var formattedErrors = errors.ToDictionary(k => k.Key, v => v.Value.ToArray());
            return (null, ValidationResult.Failed(formattedErrors), false);
        }

        // Check for duplicate name (case-insensitive)
        var existing = await store.GetByNameAsync(trimmedName!, cancellationToken);
        if (existing is not null)
        {
            var conflictErrors = new Dictionary<string, string[]>
            {
                ["Name"] = [$"A repository with the name '{trimmedName}' already exists."]
            };
            return (null, ValidationResult.Failed(conflictErrors), true);
        }

        var repository = new Repository(
            Id: Guid.NewGuid(),
            Name: trimmedName!,
            Description: string.IsNullOrWhiteSpace(trimmedDescription) ? null : trimmedDescription,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            SourceType: trimmedSourceType!,
            RepositoryUrl: trimmedUrl);

        var created = await store.AddAsync(repository, cancellationToken);
        return (created, ValidationResult.Success(), false);
    }

    private static void AddError(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
        {
            list = new List<string>();
            errors[field] = list;
        }
        list.Add(message);
    }
}

