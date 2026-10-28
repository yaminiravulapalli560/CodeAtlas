namespace CodeAtlas.Modules.CodeAnalysis.Services;

using CodeAtlas.Modules.CodeAnalysis.Models;
using Microsoft.Extensions.Logging;

public sealed record InspectionOutcome(
    bool Success,
    FolderInspectionResult? Result,
    string? ErrorMessage,
    int StatusCode);

public interface IFolderInspectionService
{
    Task<InspectionOutcome> InspectDirectoryAsync(FolderInspectionRequest request, CancellationToken cancellationToken = default);
}

public sealed class FolderInspectionService(
    IWorkspacePathValidator pathValidator,
    ILogger<FolderInspectionService> logger) : IFolderInspectionService
{
    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        "node_modules",
        "bin",
        "obj",
        "dist",
        ".vs",
        ".idea",
        ".vscode"
    };

    public Task<InspectionOutcome> InspectDirectoryAsync(
        FolderInspectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = pathValidator.ValidatePath(request.DirectoryPath, mustBeDirectory: true);
        if (!validation.IsValid)
        {
            return Task.FromResult(new InspectionOutcome(
                false,
                null,
                validation.ErrorMessage,
                validation.StatusCode));
        }

        var targetFullPath = validation.CanonicalPath!;
        var relativeFilePaths = new List<string>();
        var extensionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var targetDirInfo = new DirectoryInfo(targetFullPath);
            TraverseDirectory(targetDirInfo, targetFullPath, relativeFilePaths, extensionCounts, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access while reading directory '{Path}'", targetFullPath);
            return Task.FromResult(new InspectionOutcome(false, null, "Access to one or more directory paths was denied.", 403));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while enumerating directory '{Path}'", targetFullPath);
            return Task.FromResult(new InspectionOutcome(false, null, "Failed to inspect directory due to a filesystem error.", 500));
        }

        // Sort file paths for deterministic output
        relativeFilePaths.Sort(StringComparer.OrdinalIgnoreCase);

        // Compute observed project classifications
        var classifications = DetermineProjectClassifications(extensionCounts, relativeFilePaths);

        var result = new FolderInspectionResult(
            InspectedPath: validation.RelativeDisplayPath!,
            TotalFiles: relativeFilePaths.Count,
            ExtensionCounts: extensionCounts,
            RelativeFilePaths: relativeFilePaths,
            ProjectClassifications: classifications);

        return Task.FromResult(new InspectionOutcome(true, result, null, 200));
    }

    private void TraverseDirectory(
        DirectoryInfo currentDir,
        string baseDirectory,
        List<string> relativeFilePaths,
        Dictionary<string, int> extensionCounts,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Enumerate files in current directory
        foreach (var file in currentDir.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!pathValidator.IsSafeFileSystemEntry(file))
            {
                logger.LogWarning("Skipping unsafe reparse point file: '{File}'", file.FullName);
                continue;
            }

            var relativePath = Path.GetRelativePath(baseDirectory, file.FullName).Replace('\\', '/');
            relativeFilePaths.Add(relativePath);

            var ext = file.Extension.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext))
            {
                ext = "(no extension)";
            }

            extensionCounts[ext] = extensionCounts.GetValueOrDefault(ext, 0) + 1;
        }

        // Recurse into subdirectories that are not ignored and safe
        foreach (var subDir in currentDir.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IgnoredDirectoryNames.Contains(subDir.Name))
            {
                continue;
            }

            if (!pathValidator.IsSafeFileSystemEntry(subDir))
            {
                logger.LogWarning("Skipping unsafe reparse point directory: '{SubDir}'", subDir.FullName);
                continue;
            }

            TraverseDirectory(subDir, baseDirectory, relativeFilePaths, extensionCounts, cancellationToken);
        }
    }

    private static IReadOnlyList<string> DetermineProjectClassifications(
        Dictionary<string, int> extensionCounts,
        List<string> relativeFilePaths)
    {
        var classifications = new List<string>();

        bool hasDotNet = extensionCounts.ContainsKey(".cs") ||
                         extensionCounts.ContainsKey(".csproj") ||
                         extensionCounts.ContainsKey(".sln") ||
                         extensionCounts.ContainsKey(".slnx") ||
                         extensionCounts.ContainsKey(".fs") ||
                         extensionCounts.ContainsKey(".fsproj");

        bool hasJsTs = extensionCounts.ContainsKey(".ts") ||
                       extensionCounts.ContainsKey(".tsx") ||
                       extensionCounts.ContainsKey(".js") ||
                       extensionCounts.ContainsKey(".jsx") ||
                       relativeFilePaths.Any(p => p.EndsWith("package.json", StringComparison.OrdinalIgnoreCase));

        bool hasHtmlCss = extensionCounts.ContainsKey(".html") ||
                          extensionCounts.ContainsKey(".htm") ||
                          extensionCounts.ContainsKey(".css") ||
                          extensionCounts.ContainsKey(".scss") ||
                          extensionCounts.ContainsKey(".less");

        bool hasPython = extensionCounts.ContainsKey(".py");

        if (hasDotNet)
        {
            classifications.Add(".NET");
        }
        if (hasJsTs)
        {
            classifications.Add("JavaScript / TypeScript");
        }
        if (hasHtmlCss)
        {
            classifications.Add("HTML / CSS");
        }
        if (hasPython)
        {
            classifications.Add("Python");
        }

        if (classifications.Count == 0 && relativeFilePaths.Count > 0)
        {
            classifications.Add("Generic File Collection");
        }

        return classifications;
    }
}
