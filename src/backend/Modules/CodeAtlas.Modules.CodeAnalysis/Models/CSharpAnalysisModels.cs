namespace CodeAtlas.Modules.CodeAnalysis.Models;

public sealed record CSharpAnalysisRequest(string? FilePath);

public sealed record CSharpSymbol(
    string Name,
    string Kind,
    string? ContainingType,
    int StartLine,
    int EndLine);

public sealed record CSharpDiagnostic(
    string Message,
    string Severity,
    int StartLine,
    int StartColumn);

public sealed record CSharpAnalysisResult(
    string InspectedPath,
    string? Namespace,
    IReadOnlyList<CSharpSymbol> Symbols,
    IReadOnlyList<CSharpDiagnostic> Diagnostics);