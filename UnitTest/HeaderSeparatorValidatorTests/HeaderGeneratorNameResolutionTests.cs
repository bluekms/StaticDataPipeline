using Microsoft.Extensions.Logging;
using StaticDataHeaderGenerator.OptionHandlers;
using StaticDataHeaderGenerator.ProgramOptions;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.HeaderSeparatorValidatorTests;

[Collection("SerilogGlobal")]
public class HeaderGeneratorNameResolutionTests(ITestOutputHelper testOutputHelper)
{
    private const string TwoNamespacesSource = """
        using Sdp.Attributes;

        namespace A
        {
            [StaticDataRecord("Book", "A")]
            public sealed partial record ItemRecord([Key] int Id, [ColumnName("Item Name")] string Name);
        }

        namespace B
        {
            [StaticDataRecord("Book", "B")]
            public sealed partial record ItemRecord([Key] int Id);
        }
        """;

    [Fact]
    public void AmbiguousSimpleName_ListsCandidates()
    {
        var logger = CreateLogger();
        using var dir = new CsvTestDirectory();
        dir.Write("Records.cs", TwoNamespacesSource);

        var ex = Assert.Throws<ArgumentException>(() => GenerateHeaderHandler.Generate(new GenerateHeaderOptions
        {
            RecordCsPath = dir.Path,
            RecordName = "ItemRecord",
            Separator = "\t",
        }));

        Assert.Contains("A.ItemRecord", ex.Message, StringComparison.Ordinal);
        Assert.Contains("B.ItemRecord", ex.Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void FullName_AndEscapedSeparator_AndExtensionDefault()
    {
        var logger = CreateLogger();
        using var dir = new CsvTestDirectory();
        dir.Write("Records.cs", TwoNamespacesSource);
        var outputFileName = Path.Combine(dir.Path, "out.");

        var exitCode = GenerateHeaderHandler.Generate(new GenerateHeaderOptions
        {
            RecordCsPath = dir.Path,
            RecordName = "A.ItemRecord",
            Separator = "\\t",
            OutputFileName = outputFileName,
        });

        Assert.Equal(0, exitCode);
        var written = File.ReadAllText(Path.Combine(dir.Path, "out.md"));
        Assert.Contains("## A.ItemRecord", written, StringComparison.Ordinal);
        Assert.Contains("### Headers (TSV)", written, StringComparison.Ordinal);
        Assert.Contains("Id\tItem Name", written, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<HeaderGeneratorNameResolutionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<HeaderGeneratorNameResolutionTests>() is not TestOutputLogger<HeaderGeneratorNameResolutionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
