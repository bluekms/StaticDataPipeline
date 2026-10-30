using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class SwitchForeignKeyTypedConditionEmissionTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string Template =
        """
        using Microsoft.Extensions.Logging;
        using Sdp.Attributes;
        using Sdp.Manager;
        using Sdp.Table;
        using System;
        using System.Collections.Immutable;

        namespace Test;

        public enum RewardKind
        {
            Item = 0,
            Character = 1,
        }

        [StaticDataRecord("Target", "S")]
        public sealed partial record TargetRec(int Id);

        public sealed partial class TargetTable : StaticDataTable<TargetRec>;

        [StaticDataRecord("Consumer", "S")]
        public sealed partial record ConsumerRec(
            int Id,
            {{CONDITION_PARAMETER}},
            [SwitchForeignKey("Kind", "{{CONDITION_VALUE}}", "Target", "Id")] int RefId);

        public sealed partial class ConsumerTable : StaticDataTable<ConsumerRec>;

        public partial class GenManager(ILogger logger)
            : StaticDataManager<GenManager.TableSet>(logger)
        {
            public sealed partial record TableSet(TargetTable? Target, ConsumerTable? Consumer);
        }
        """;

    [Theory]
    [InlineData("RewardKind Kind", "Character", "__r.Kind == global::Test.RewardKind.Character")]
    [InlineData("RewardKind Kind", "1", "__r.Kind == (global::Test.RewardKind)(1)")]
    [InlineData("int Kind", "7", "__r.Kind == 7")]
    [InlineData("ulong Kind", "18446744073709551615", "__r.Kind == 18446744073709551615UL")]
    [InlineData("bool Kind", "true", "__r.Kind == true")]
    [InlineData("char Kind", "A", "__r.Kind == 'A'")]
    [InlineData("string Kind", "Gold", "__r.Kind == \"Gold\"")]
    [InlineData("Guid Kind", "{0F8FAD5B-D9CB-469F-A165-70867728950E}", "__r.Kind == new global::System.Guid(\"0f8fad5b-d9cb-469f-a165-70867728950e\")")]
    [InlineData("decimal Kind", "1.50", "__r.Kind == 1.50m")]
    [InlineData("double Kind", "1.5", "__r.Kind == 1.5d")]
    [InlineData("float Kind", "2", "__r.Kind == 2f")]
    [InlineData("[DateTimeFormat(\"yyyy-MM-dd\")] DateTime Kind", "2024-01-02", "__r.Kind == new global::System.DateTime(638397504000000000L, global::System.DateTimeKind.Unspecified)")]
    [InlineData("[TimeSpanFormat(\"hh\\\\:mm\")] TimeSpan Kind", "01:30", "__r.Kind == new global::System.TimeSpan(54000000000L)")]
    [InlineData("[DateTimeFormat(\"yyyy-MM-dd\")] DateOnly Kind", "2024-01-02", "__r.Kind == new global::System.DateOnly(2024, 1, 2)")]
    [InlineData("[DateTimeFormat(\"HH:mm\")] TimeOnly Kind", "01:30", "__r.Kind == new global::System.TimeOnly(54000000000L)")]
    public void Emits_typed_literal_comparison(string conditionParameter, string conditionValue, string expected)
    {
        var logger = CreateLogger();

        var source = Template
            .Replace("{{CONDITION_PARAMETER}}", conditionParameter, StringComparison.Ordinal)
            .Replace("{{CONDITION_VALUE}}", conditionValue, StringComparison.Ordinal);

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains(expected, generated, StringComparison.Ordinal);
        Assert.DoesNotContain("ToString(null, global::System.Globalization.CultureInfo.InvariantCulture)", generated, StringComparison.Ordinal);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(output.Final);
        Assert.Empty(errors);
    }

    private TestOutputLogger<SwitchForeignKeyTypedConditionEmissionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyTypedConditionEmissionTests>()
            is not TestOutputLogger<SwitchForeignKeyTypedConditionEmissionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
