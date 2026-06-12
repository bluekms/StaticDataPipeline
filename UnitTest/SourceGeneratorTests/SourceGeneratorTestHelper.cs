using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sdp.Attributes;
using Sdp.SourceGenerator;

namespace UnitTest.SourceGeneratorTests;

internal sealed record GeneratorRunOutput(GeneratorDriverRunResult Run, CSharpCompilation Final);

internal static class SourceGeneratorTestHelper
{
    public static GeneratorDriverRunResult Run(string source)
        => RunWithFinal(source).Run;

    public static GeneratorRunOutput RunWithFinal(string source)
        => RunWithFinal(source, LanguageVersion.Latest);

    // langVersion 으로 소비자 컴파일레이션의 C# 버전을 내릴 수 있다.
    // 유니티(C# 9) 호환 검증이 생성 코드 문법을 이 게이트로 잡는다.
    public static GeneratorRunOutput RunWithFinal(string source, LanguageVersion langVersion)
    {
        var parseOptions = new CSharpParseOptions(langVersion);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var trusted = ((string?)System.AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator) ?? [];

        var references = trusted
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(typeof(StaticDataRecordAttribute).Assembly.Location))
            .Append(MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "SgTestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var generator = new SdpIncrementalGenerator().AsSourceGenerator();

        // 생성 트리도 같은 언어 버전으로 파싱되도록 driver 에 parse options 를 전달한다.
        // 버전이 다르면 RunGeneratorsAndUpdateCompilation 이 ArgumentException 을 던진다.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var finalCompilation, out _);
        return new GeneratorRunOutput(driver.GetRunResult(), (CSharpCompilation)finalCompilation);
    }

    public static SyntaxTree GetSingleTree(GeneratorDriverRunResult result, string fileNameSuffix)
        => Assert.Single(
            result.GeneratedTrees,
            t => t.FilePath.EndsWith(fileNameSuffix, StringComparison.Ordinal));

    public static List<Diagnostic> GetCompilationErrors(CSharpCompilation final)
        => final
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
}
