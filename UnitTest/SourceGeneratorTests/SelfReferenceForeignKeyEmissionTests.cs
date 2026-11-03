using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class SelfReferenceForeignKeyEmissionTests(ITestOutputHelper testOutputHelper)
{
    private const string RecordsLocal = "var __records = tableSet.Quest.Records;";
    private const string RecordsAccess = "tableSet.Quest.Records";

    [Fact]
    public void Self_reference_foreign_key_compiles_without_warnings()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            using Microsoft.Extensions.Logging;
            using Sdp.Attributes;
            using Sdp.Manager;
            using Sdp.Table;
            using System.Collections.Immutable;

            namespace Test;

            [StaticDataRecord("Quest", "S")]
            public sealed partial record QuestRec(
                [Key] int Id,
                [NullString("")] [ForeignKey("Quest", "Id")] int? PrevQuestId);

            public sealed partial class QuestTable : StaticDataTable<QuestRec>;

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(QuestTable? Quest);
            }
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains(RecordsLocal, generated, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(generated, RecordsAccess));
        Assert.Contains(".Add(__t.Id);", generated, StringComparison.Ordinal);

        var warningsAndErrors = SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(output.Final);
        Assert.Empty(warningsAndErrors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Self_reference_foreign_key_to_branding_key_compiles_without_warnings()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            using Microsoft.Extensions.Logging;
            using Sdp.Attributes;
            using Sdp.Manager;
            using Sdp.Table;
            using System.Collections.Immutable;

            namespace Test;

            [StaticDataRecord("Quest", "S")]
            public sealed partial record QuestRec(
                [Key] QuestRec.QuestId Id,
                [NullString("")] [ForeignKey("Quest", "Id")] int? ParentId)
            {
                public readonly record struct QuestId(int Value);
            }

            public sealed partial class QuestTable : StaticDataTable<QuestRec>;

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(QuestTable? Quest);
            }
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains(RecordsLocal, generated, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(generated, RecordsAccess));
        Assert.Contains(".Add(__t.Id.Value);", generated, StringComparison.Ordinal);

        var warningsAndErrors = SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(output.Final);
        Assert.Empty(warningsAndErrors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Self_reference_switch_foreign_key_compiles_without_warnings()
    {
        var logger = CreateLogger();

        // language=C#
        const string source =
            """
            using Microsoft.Extensions.Logging;
            using Sdp.Attributes;
            using Sdp.Manager;
            using Sdp.Table;
            using System.Collections.Immutable;

            namespace Test;

            public enum LinkKind
            {
                Quest,
                Item,
            }

            [StaticDataRecord("Item", "S")]
            public sealed partial record ItemRec([Key] int Id);

            public sealed partial class ItemTable : StaticDataTable<ItemRec>;

            [StaticDataRecord("Quest", "S")]
            public sealed partial record QuestRec(
                [Key] int Id,
                LinkKind Kind,
                [SwitchForeignKey("Kind", "Quest", "Quest", "Id")]
                [SwitchForeignKey("Kind", "Item", "Item", "Id")]
                int LinkId);

            public sealed partial class QuestTable : StaticDataTable<QuestRec>;

            public partial class GenManager(ILogger logger)
                : StaticDataManager<GenManager.TableSet>(logger)
            {
                public sealed partial record TableSet(ItemTable? Item, QuestTable? Quest);
            }
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains(RecordsLocal, generated, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(generated, RecordsAccess));
        Assert.Contains("foreach (var __t in tableSet.Item.Records)", generated, StringComparison.Ordinal);

        var warningsAndErrors = SourceGeneratorTestHelper.GetCompilationWarningsAndErrors(output.Final);
        Assert.Empty(warningsAndErrors);
        Assert.Empty(logger.Logs);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = text.IndexOf(value, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private TestOutputLogger<SelfReferenceForeignKeyEmissionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SelfReferenceForeignKeyEmissionTests>()
            is not TestOutputLogger<SelfReferenceForeignKeyEmissionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
