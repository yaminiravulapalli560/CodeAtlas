namespace CodeAtlas.Modules.CodeAnalysis.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public sealed record PathValidationResult(
    bool IsValid,
    string? CanonicalPath,
    string? RelativeDisplayPath,
    string? ErrorMessage,
    int StatusCode)
{
    public static PathValidationResult Success(string canonicalPath, string relativeDisplayPath) =>
        new(true, canonicalPath, relativeDisplayPath, null, 200);

    public static PathValidationResult Failure(string errorMessage, int statusCode) =>
        new(false, null, null, errorMessage, statusCode);
}

public interface IWorkspacePathValidator
{
    string GetWorkspaceRoot();
    PathValidationResult ValidatePath(string? requestedPath, bool mustBeDirectory = true);
    bool IsSafeFileSystemEntry(FileSystemInfo entry);
}

public sealed class WorkspacePathValidator : IWorkspacePathValidator
{
    private readonly string _workspaceRoot;
    private readonly ILogger<WorkspacePathValidator> _logger;

    public WorkspacePathValidator(IConfiguration configuration, ILogger<WorkspacePathValidator> logger)
    {
        _logger = logger;
        var configuredRoot = configuration["Workspace:RootPath"] ?? ".";
        try
        {
            _workspaceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(configuredRoot));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve configured workspace root path: {ConfiguredRoot}", configuredRoot);
            _workspaceRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath("."));
        }
    }

    public string GetWorkspaceRoot() => _workspaceRoot;

    public PathValidationResult ValidatePath(string? requestedPath, bool mustBeDirectory = true)
    {
        var input = requestedPath?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            input = ".";
        }

        string targetFullPath;
        try
        {
            if (Path.IsPathRooted(input))
            {
                targetFullPath = Path.GetFullPath(input);
            }
            else
            {
                targetFullPath = Path.GetFullPath(Path.Combine(_workspaceRoot, input));
            }
        }
        catch (Exception)
        {
            return PathValidationResult.Failure("The specified path format is invalid.", 400);
        }

        targetFullPath = Path.TrimEndingDirectorySeparator(targetFullPath);

        // String prefix boundary check
        if (!IsWithinRoot(targetFullPath))
        {
            _logger.LogWarning("Access denied: path '{Target}' is outside allowed root '{Root}'", targetFullPath, _workspaceRoot);
            return PathValidationResult.Failure("Access denied: Path is outside the configured workspace root boundary.", 403);
        }

        // Verify existence
        if (mustBeDirectory)
        {
            if (!Directory.Exists(targetFullPath))
            {
                return PathValidationResult.Failure($"Directory not found: '{input}'.", 404);
            }

            var dirInfo = new DirectoryInfo(targetFullPath);
            if (!IsSafeFileSystemEntry(dirInfo))
            {
                _logger.LogWarning("Access denied: directory '{Target}' is or contains an unsafe reparse point.", targetFullPath);
                return PathValidationResult.Failure("Access denied: Reparse point, junction, or symbolic link outside allowed workspace boundary.", 403);
            }
        }
        else
        {
            if (!File.Exists(targetFullPath) && !Directory.Exists(targetFullPath))
            {
                return PathValidationResult.Failure($"Path not found: '{input}'.", 404);
            }

            var fileInfo = new FileInfo(targetFullPath);
            if (fileInfo.Exists && !IsSafeFileSystemEntry(fileInfo))
            {
                _logger.LogWarning("Access denied: file '{Target}' is an unsafe reparse point.", targetFullPath);
                return PathValidationResult.Failure("Access denied: Reparse point or symbolic link outside allowed workspace boundary.", 403);
            }
        }

        var relativeDisplay = Path.GetRelativePath(_workspaceRoot, targetFullPath).Replace('\\', '/');
        if (relativeDisplay == ".")
        {
            relativeDisplay = "./";
        }

        return PathValidationResult.Success(targetFullPath, relativeDisplay);
    }

    public bool IsSafeFileSystemEntry(FileSystemInfo entry)
    {
        try
        {
            if (!entry.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                return true;
            }

            // If it is a reparse point (symlink, junction), resolve target and ensure it remains strictly inside _workspaceRoot
            var target = entry.ResolveLinkTarget(returnFinalTarget: true);
            if (target is null)
            {
                // Inability to resolve or dangling link: reject for safety
                return false;
            }

            var resolvedCanonical = Path.TrimEndingDirectorySeparator(Path.GetFullPath(target.FullName));
            return IsWithinRoot(resolvedCanonical);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve link target for entry '{Entry}'", entry.FullName);
            return false;
        }
    }

    private bool IsWithinRoot(string canonicalPath)
    {
        return canonicalPath.Equals(_workspaceRoot, StringComparison.OrdinalIgnoreCase) ||
               canonicalPath.StartsWith(_workspaceRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}

