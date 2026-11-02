using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class ForeignKeyBrandingTargetEmissionTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string Template =
        """
        using Microsoft.Extensions.Logging;
        using Sdp.Attributes;
        using Sdp.Manager;
        using Sdp.Table;
        using System.Collections.Immutable;

        namespace Test;

        [StaticDataRecord("Item", "S")]
        public sealed partial record ItemRec([Key] ItemRec.ItemId Id, string Name)
        {
            public readonly record struct ItemId(int Value);
        }

        public sealed partial class ItemTable : StaticDataTable<ItemRec>;

        [StaticDataRecord("Consumer", "S")]
        public sealed partial record ConsumerRec(
            int Id,
            [ForeignKey("Item", "Id")] {{COLUMN_TYPE}} ItemId);

        public sealed partial class ConsumerTable : StaticDataTable<ConsumerRec>;

        public partial class GenManager(ILogger logger)
            : StaticDataManager<GenManager.TableSet>(logger)
        {
            public sealed partial record TableSet(ItemTable? Item, ConsumerTable? Consumer);
        }
        """;

    [Fact]
    public void Primitive_column_collects_branding_key_inner_value()
    {
        var logger = CreateLogger();

        var source = Template.Replace("{{COLUMN_TYPE}}", "int", StringComparison.Ordinal);

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains(".Add(__t.Id.Value);", generated, StringComparison.Ordinal);
        Assert.Contains("global::System.Collections.Generic.HashSet<int>", generated, StringComparison.Ordinal);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(output.Final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Branding_column_collects_branding_key_as_is()
    {
        var logger = CreateLogger();

        var source = Template.Replace("{{COLUMN_TYPE}}", "ItemRec.ItemId", StringComparison.Ordinal);

        var output = SourceGeneratorTestHelper.RunWithFinal(source);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains(".Add(__t.Id);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(".Add(__t.Id.Value);", generated, StringComparison.Ordinal);

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(output.Final);
        Assert.Empty(errors);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<ForeignKeyBrandingTargetEmissionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyBrandingTargetEmissionTests>()
            is not TestOutputLogger<ForeignKeyBrandingTargetEmissionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
