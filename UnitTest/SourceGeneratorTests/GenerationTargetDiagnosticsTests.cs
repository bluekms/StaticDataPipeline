using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public partial class GenerationTargetDiagnosticsTests(ITestOutputHelper testOutputHelper)
{
    private const string Header =
        """
        using Microsoft.Extensions.Logging;
        using Sdp.Attributes;
        using Sdp.Manager;
        using Sdp.Table;
        using System;
        using System.Collections.Immutable;

        namespace Test;

        [StaticDataRecord("Item", "S")]
        public sealed partial record ItemRec([Key] int Id, string Name);

        """;

    private enum BigFlag : ulong
    {
        One = 1,
        All = ulong.MaxValue,
    }

    [StaticDataRecord("Big", "S")]
    private partial record BigRecord([Key] int Id, BigFlag Flag);

    [Fact]
    public void TableInsideGenericOuter_ReportsSdp0012()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public partial class Holder<T>
            {
                public sealed partial class ItemTable : StaticDataTable<ItemRec>;
            }
            """;

        var diagnostics = RunDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SDP0012" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void UlongEnumMemberBeyondLongMax_IsGeneratedAndParsed()
    {
        var logger = CreateLogger();

        var records = CsvLoader.Parse("Id,Flag\n1,18446744073709551615\n2,One", BigRecord.MapFromCsvRow);

        Assert.Equal(BigFlag.All, records[0].Flag);
        Assert.Equal(BigFlag.One, records[1].Flag);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DateOnlyTypedRange_WithUnparsableBound_ReportsSdp0010()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Day", "S")]
            public sealed partial record DayRec(
                [Key] int Id,
                [Range(typeof(DateOnly), "2024-13-45", "2024-01-01")] [DateTimeFormat("yyyy-MM-dd")] DateOnly Day);
            """;

        var diagnostics = RunDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SDP0010" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SwitchForeignKeyConditionOnNonPositionalProperty_ReportsSdp0209()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            [StaticDataRecord("Consumer", "S")]
            public sealed partial record ConsumerRec(
                [Key] int Id,
                [SwitchForeignKey("Kind", "1", "Item", "Id")] int RefId)
            {
                public int Kind => Id;
            }

            public sealed partial class ItemTable : StaticDataTable<ItemRec>;

            public sealed partial class ConsumerTable : StaticDataTable<ConsumerRec>;

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(ItemTable? Item, ConsumerTable? Consumer);
            }
            """;

        var diagnostics = RunDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "SDP0209");
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void GeneratedLoaders_UseConfigureAwaitFalse()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            public sealed partial class ItemTable : StaticDataTable<ItemRec>;

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(ItemTable Item);
            }
            """;

        var run = SourceGeneratorTestHelper.Run(Header + source);
        var tableFactory = SourceGeneratorTestHelper.GetSingleTree(run, "ItemTable.TableFactory.g.cs").ToString();
        var loader = SourceGeneratorTestHelper.GetSingleTree(run, "TableSet.TableSetLoader.g.cs").ToString();

        Assert.Contains(".ConfigureAwait(false)", tableFactory, StringComparison.Ordinal);
        Assert.Contains("Task.WhenAll(__t0).ConfigureAwait(false)", loader, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    private static List<Diagnostic> RunDiagnostics(string consumerSource)
        => SourceGeneratorTestHelper.Run(Header + consumerSource)
            .Results
            .SelectMany(r => r.Diagnostics)
            .ToList();

    private TestOutputLogger<GenerationTargetDiagnosticsTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<GenerationTargetDiagnosticsTests>() is not TestOutputLogger<GenerationTargetDiagnosticsTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
