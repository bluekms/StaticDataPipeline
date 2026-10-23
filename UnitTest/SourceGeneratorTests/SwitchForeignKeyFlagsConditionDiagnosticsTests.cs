using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class SwitchForeignKeyFlagsConditionDiagnosticsTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string Header =
        """
        using Microsoft.Extensions.Logging;
        using Sdp.Attributes;
        using Sdp.Manager;
        using Sdp.Table;
        using System.Collections.Immutable;

        namespace Test;

        [System.Flags]
        public enum Access
        {
            None = 0,
            Read = 1,
            Write = 2,
            Execute = 4,
            ReadWrite = Read | Write,
        }

        [StaticDataRecord("Target", "S")]
        public sealed partial record TargetRec(int Id, string Name);

        public sealed partial class TargetTable(ImmutableArray<TargetRec> records)
            : StaticDataTable<TargetRec>(records);

        """;

    // language=C#
    private const string ConsumerTemplate =
        """
        [StaticDataRecord("Consumer", "S")]
        public sealed partial record ConsumerRec(
            int Id,
            Access Access,
            [SwitchForeignKey("Access", "{COND}", "Target", "Id")] int RefId);

        public sealed partial class ConsumerTable(ImmutableArray<ConsumerRec> records)
            : StaticDataTable<ConsumerRec>(records);

        public partial class GenManager(ILogger logger)
            : StaticDataManager<GenManager.TableSet>(logger)
        {
            public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
        }
        """;

    private static GeneratorDriverRunResult RunWithConditionValue(string conditionValue)
    {
        var consumerSource = ConsumerTemplate.Replace("{COND}", conditionValue, StringComparison.Ordinal);

        return SourceGeneratorTestHelper.Run(Header + consumerSource);
    }

    [Theory]
    [InlineData("Read")]
    [InlineData("ReadWrite")]
    [InlineData("Read, Write")]
    [InlineData("Write,Read")]
    [InlineData("5")]
    [InlineData("99")]
    public void DoesNotReport_SDP0212_when_flags_condition_value_is_resolvable(string conditionValue)
    {
        var logger = CreateLogger();

        var diagnostics = RunWithConditionValue(conditionValue)
            .Results
            .SelectMany(r => r.Diagnostics)
            .ToList();
        Assert.DoesNotContain(diagnostics, d => d.Id == "SDP0212");
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("Weapno")]
    [InlineData("Read|Write")]
    [InlineData("Read, Weapno")]
    public void Reports_SDP0212_as_error_when_flags_condition_value_is_unresolvable(string conditionValue)
    {
        var logger = CreateLogger();

        var diagnostics = RunWithConditionValue(conditionValue)
            .Results
            .SelectMany(r => r.Diagnostics)
            .ToList();
        Assert.Contains(diagnostics, d => d.Id == "SDP0212" && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("Read, Write", "(global::Test.Access)(3)")]
    [InlineData("5", "(global::Test.Access)(5)")]
    public void Emits_cast_value_comparison_for_flags_condition_value(string conditionValue, string expectedLiteral)
    {
        var logger = CreateLogger();

        var generatedTexts = RunWithConditionValue(conditionValue)
            .Results
            .SelectMany(r => r.GeneratedSources)
            .Select(s => s.SourceText.ToString())
            .ToList();
        Assert.Contains(generatedTexts, text => text.Contains(expectedLiteral, StringComparison.Ordinal));
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<SwitchForeignKeyFlagsConditionDiagnosticsTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyFlagsConditionDiagnosticsTests>()
            is not TestOutputLogger<SwitchForeignKeyFlagsConditionDiagnosticsTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
