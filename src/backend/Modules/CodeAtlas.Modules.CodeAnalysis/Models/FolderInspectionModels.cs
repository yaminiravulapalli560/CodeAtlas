namespace CodeAtlas.Modules.CodeAnalysis.Models;

public sealed record FolderInspectionRequest(string? DirectoryPath);

public sealed record FolderInspectionResult(
    string InspectedPath,
    int TotalFiles,
    IReadOnlyDictionary<string, int> ExtensionCounts,
    IReadOnlyList<string> RelativeFilePaths,
    IReadOnlyList<string> ProjectClassifications);

