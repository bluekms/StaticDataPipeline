using System.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner;

public static class RecordSchemaLoader
{
    public sealed record Result(SemanticModel SemanticModel, List<RecordDeclarationSyntax> RecordDeclarationList, List<EnumDeclarationSyntax> EnumDeclarationList);

    private static readonly string[] SkipCompileErrorIds =
    [
        "CS1031",
        "CS1001",
        "CS0518",
        "CS0246",
        "CS1729",
        "CS5001",
        "CS0103",
        "CS8019",
        "CS8632",
    ];

    public static IReadOnlyList<Result> Load(string csPath, ILogger logger)
    {
        var results = new List<Result>();

        if (File.Exists(csPath))
        {
            var code = File.ReadAllText(csPath);
            results.Add(OnLoad(code, logger));
        }
        else if (Directory.Exists(csPath))
        {
            var files = Directory.GetFiles(csPath, "*.cs");
            foreach (var file in files)
            {
                var code = File.ReadAllText(file);
                results.Add(OnLoad(code, logger));
            }
        }
        else
        {
            throw new ArgumentException("The file or directory does not exist.", nameof(csPath));
        }

        return results;
    }

    public static async Task<IReadOnlyList<Result>> LoadAsync(string csPath, ILogger logger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<Result>();

        if (File.Exists(csPath))
        {
            var code = await File.ReadAllTextAsync(csPath, cancellationToken);
            results.Add(OnLoad(code, logger));
        }
        else if (Directory.Exists(csPath))
        {
            var files = Directory.GetFiles(csPath, "*.cs");
            foreach (var file in files)
            {
                var code = await File.ReadAllTextAsync(file, cancellationToken);
                results.Add(OnLoad(code, logger));
            }
        }
        else
        {
            throw new ArgumentException("The file or directory does not exist.", nameof(csPath));
        }

        return results;
    }

    internal static Result OnLoad(string code, ILogger logger)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();
        var compilation = CSharpCompilation.Create("SchemaInfoScanner", [syntaxTree]);

        var result = compilation.GetDiagnostics();
        var compileErrors = result
            .Where(x => !SkipCompileErrorIds.Contains(x.Id))
            .ToList();

        if (compileErrors.Count is not 0)
        {
            LogException(logger, Messages.CodeNotCompilable(code), null);
            foreach (var error in compileErrors)
            {
                LogException(logger, error.ToString(), null);
            }

            throw new SyntaxErrorException($"{compileErrors.Count} compile errors occurred.");
        }

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var recordDeclarationList = root.DescendantNodes().OfType<RecordDeclarationSyntax>().ToList();
        var enumDeclarationList = root.DescendantNodes().OfType<EnumDeclarationSyntax>().ToList();

        return new(semanticModel, recordDeclarationList, enumDeclarationList);
    }

    private static readonly Action<ILogger, string, Exception?> LogException =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(LogException)), "{Message}");
}
