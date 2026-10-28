
namespace CodeAtlas.Modules.CodeAnalysis.Services;

using CodeAtlas.Modules.CodeAnalysis.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

public sealed record CSharpAnalysisOutcome(
    bool Success,
    CSharpAnalysisResult? Result,
    string? ErrorMessage,
    int StatusCode);

public interface ICSharpSourceAnalysisService
{
    Task<CSharpAnalysisOutcome> AnalyzeFileAsync(
        CSharpAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CSharpSourceAnalysisService(
    IWorkspacePathValidator pathValidator,
    ILogger<CSharpSourceAnalysisService> logger)
    : ICSharpSourceAnalysisService
{
    private const long MaxFileSizeBytes = 1_048_576;

    public async Task<CSharpAnalysisOutcome> AnalyzeFileAsync(
        CSharpAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validation = pathValidator.ValidatePath(
            request.FilePath,
            mustBeDirectory: false);

        if (!validation.IsValid)
        {
            return Failure(validation.ErrorMessage, validation.StatusCode);
        }

        var path = validation.CanonicalPath!;

        if (Directory.Exists(path))
        {
            return Failure("A C# source file is required, not a directory.", 400);
        }

        if (!string.Equals(
                Path.GetExtension(path),
                ".cs",
                StringComparison.OrdinalIgnoreCase))
        {
            return Failure("Only .cs files can be analyzed.", 400);
        }

        try
        {
            var fileInfo = new FileInfo(path);

            if (!fileInfo.Exists)
            {
                return Failure("The requested source file was not found.", 404);
            }

            if (!pathValidator.IsSafeFileSystemEntry(fileInfo))
            {
                return Failure("Access to this source file is restricted.", 403);
            }

            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return Failure("The source file exceeds the 1 MB analysis limit.", 413);
            }

            var source = await File.ReadAllTextAsync(path, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            var syntaxTree = CSharpSyntaxTree.ParseText(
                source,
                cancellationToken: cancellationToken);

            var root = await syntaxTree.GetRootAsync(cancellationToken);

            var symbols = new List<CSharpSymbol>();

            foreach (var node in root.DescendantNodesAndSelf())
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (node)
                {
                    case BaseNamespaceDeclarationSyntax ns:
                        symbols.Add(CreateSymbol(ns, "Namespace", null));
                        break;

                    case ClassDeclarationSyntax type:
                        symbols.Add(CreateSymbol(type, "Class", GetContainingType(type)));
                        break;

                    case InterfaceDeclarationSyntax type:
                        symbols.Add(CreateSymbol(type, "Interface", GetContainingType(type)));
                        break;

                    case StructDeclarationSyntax type:
                        symbols.Add(CreateSymbol(type, "Struct", GetContainingType(type)));
                        break;

                    case RecordDeclarationSyntax type:
                        symbols.Add(CreateSymbol(
                            type,
                            type.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)
                                ? "Record Struct"
                                : "Record",
                            GetContainingType(type)));
                        break;

                    case EnumDeclarationSyntax type:
                        symbols.Add(CreateSymbol(type, "Enum", GetContainingType(type)));
                        break;

                    case DelegateDeclarationSyntax type:
                        symbols.Add(CreateSymbol(type, "Delegate", GetContainingType(type)));
                        break;

                    case MethodDeclarationSyntax method:
                        symbols.Add(CreateSymbol(method, "Method", GetContainingType(method)));
                        break;

                    case ConstructorDeclarationSyntax constructor:
                        symbols.Add(CreateSymbol(
                            constructor,
                            "Constructor",
                            GetContainingType(constructor)));
                        break;

                    case PropertyDeclarationSyntax property:
                        symbols.Add(CreateSymbol(
                            property,
                            "Property",
                            GetContainingType(property)));
                        break;
                }
            }

            var diagnostics = syntaxTree.GetDiagnostics(cancellationToken)
                .Where(d => d.Severity == DiagnosticSeverity.Error ||
                            d.Severity == DiagnosticSeverity.Warning)
                .Select(d =>
                {
                    var line = d.Location.GetLineSpan().StartLinePosition;

                    return new CSharpDiagnostic(
                        d.GetMessage(),
                        d.Severity.ToString(),
                        line.Line + 1,
                        line.Character + 1);
                })
                .ToList();

            var namespaceName = root.DescendantNodesAndSelf()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .FirstOrDefault()?
                .Name.ToString();

            var result = new CSharpAnalysisResult(
                validation.RelativeDisplayPath!,
                namespaceName,
                symbols,
                diagnostics);

            return new CSharpAnalysisOutcome(true, result, null, 200);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Access denied while analyzing C# file '{Path}'", path);
            return Failure("Access to the source file was denied.", 403);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Unable to read C# file '{Path}'", path);
            return Failure("The source file could not be read.", 500);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to analyze C# file '{Path}'", path);
            return Failure("C# source analysis failed.", 500);
        }
    }

    private static CSharpSymbol CreateSymbol(
        SyntaxNode node,
        string kind,
        string? containingType)
    {
        var span = node.SyntaxTree.GetLineSpan(node.Span);

        return new CSharpSymbol(
            GetName(node),
            kind,
            containingType,
            span.StartLinePosition.Line + 1,
            span.EndLinePosition.Line + 1);
    }

    private static string GetName(SyntaxNode node) =>
        node switch
        {
            BaseNamespaceDeclarationSyntax ns => ns.Name.ToString(),
            ClassDeclarationSyntax type => type.Identifier.Text,
            InterfaceDeclarationSyntax type => type.Identifier.Text,
            StructDeclarationSyntax type => type.Identifier.Text,
            RecordDeclarationSyntax type => type.Identifier.Text,
            EnumDeclarationSyntax type => type.Identifier.Text,
            DelegateDeclarationSyntax type => type.Identifier.Text,
            MethodDeclarationSyntax method => method.Identifier.Text,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.Text,
            PropertyDeclarationSyntax property => property.Identifier.Text,
            _ => node.ToString()
        };

    private static string? GetContainingType(SyntaxNode node) =>
        node.Ancestors()
            .FirstOrDefault(n =>
                n is TypeDeclarationSyntax ||
                n is EnumDeclarationSyntax)
            is { } parent
                ? GetName(parent)
                : null;

    private static CSharpAnalysisOutcome Failure(string? message, int statusCode) =>
        new(false, null, message ?? "The request could not be processed.", statusCode);
}
