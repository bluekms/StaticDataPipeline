using System.Data;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner.Resources;

namespace SchemaInfoScanner;

public static class RecordSchemaLoader
{
    public sealed record Result(SemanticModel SemanticModel, List<RecordDeclarationSyntax> RecordDeclarationList);

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

    private static readonly Lazy<List<MetadataReference>> RuntimeReferences = new(CreateRuntimeReferences);

    private static readonly CSharpCompilationOptions CompilationOptions = new(OutputKind.DynamicallyLinkedLibrary);

    public static IReadOnlyList<Result> Load(string csPath, ILogger logger)
    {
        if (File.Exists(csPath))
        {
            var code = File.ReadAllText(csPath);

            return OnLoad([CSharpSyntaxTree.ParseText(code, path: csPath)], logger);
        }

        if (Directory.Exists(csPath))
        {
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var file in Directory.GetFiles(csPath, "*.cs"))
            {
                var code = File.ReadAllText(file);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: file));
            }

            return OnLoad(syntaxTrees, logger);
        }

        throw FileOrDirectoryNotExist(csPath);
    }

    public static async Task<IReadOnlyList<Result>> LoadAsync(string csPath, ILogger logger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(csPath))
        {
            var code = await File.ReadAllTextAsync(csPath, cancellationToken);

            return OnLoad([CSharpSyntaxTree.ParseText(code, path: csPath, cancellationToken: cancellationToken)], logger);
        }

        if (Directory.Exists(csPath))
        {
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var file in Directory.GetFiles(csPath, "*.cs"))
            {
                var code = await File.ReadAllTextAsync(file, cancellationToken);
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(code, path: file, cancellationToken: cancellationToken));
            }

            return OnLoad(syntaxTrees, logger);
        }

        throw FileOrDirectoryNotExist(csPath);
    }

    internal static Result OnLoad(string code, ILogger logger)
    {
        var results = OnLoad([CSharpSyntaxTree.ParseText(code)], logger);

        return results[0];
    }

    private static List<Result> OnLoad(List<SyntaxTree> syntaxTrees, ILogger logger)
    {
        var compilation = CSharpCompilation.Create("SchemaInfoScanner", syntaxTrees, RuntimeReferences.Value, CompilationOptions);

        var compileErrors = compilation.GetDiagnostics()
            .Where(x => x.Severity is DiagnosticSeverity.Error)
            .Where(x => !SkipCompileErrorIds.Contains(x.Id))
            .ToList();

        if (compileErrors.Count is not 0)
        {
            var sources = string.Join(", ", syntaxTrees.Select(DescribeSource));
            var msg = string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.CodeNotCompilable,
                sources);
            LogException(logger, msg, null);
            foreach (var error in compileErrors)
            {
                LogException(logger, error.ToString(), null);
            }

            throw new SyntaxErrorException(string.Format(
                CultureInfo.CurrentCulture,
                Messages.Composite.CompileErrorsOccurred,
                compileErrors.Count));
        }

        var results = new List<Result>(syntaxTrees.Count);
        foreach (var syntaxTree in syntaxTrees)
        {
            var root = syntaxTree.GetRoot();
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var recordDeclarationList = root.DescendantNodes().OfType<RecordDeclarationSyntax>().ToList();

            results.Add(new(semanticModel, recordDeclarationList));
        }

        return results;
    }

    private static string DescribeSource(SyntaxTree syntaxTree)
    {
        return string.IsNullOrEmpty(syntaxTree.FilePath)
            ? syntaxTree.ToString()
            : syntaxTree.FilePath;
    }

    private static List<MetadataReference> CreateRuntimeReferences()
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrEmpty(trustedAssemblies))
        {
            throw new InvalidOperationException(Messages.RuntimeAssembliesNotFound);
        }

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => (MetadataReference)MetadataReference.CreateFromFile(x))
            .ToList();
    }

    private static ArgumentException FileOrDirectoryNotExist(string csPath)
    {
        var message = string.Format(CultureInfo.CurrentCulture, Messages.Composite.FileOrDirectoryNotExist, csPath);

        return new ArgumentException(message, nameof(csPath));
    }

    private static readonly Action<ILogger, string, Exception?> LogException =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(LogException)), "{Message}");
}
