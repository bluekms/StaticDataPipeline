using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

public class ForeignKeyMessageDetailEmissionTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string Source =
        """
        using Microsoft.Extensions.Logging;
        using Sdp.Attributes;
        using Sdp.Manager;
        using Sdp.Table;
        using System.Collections.Frozen;
        using System.Collections.Immutable;

        namespace Test;

        [StaticDataRecord("Skill", "S")]
        public sealed partial record SkillRec([Key] SkillRec.SkillId Id, string Name)
        {
            public readonly record struct SkillId(int Value);
        }

        public sealed partial class SkillTable : StaticDataTable<SkillRec>;

        [StaticDataRecord("Hero", "S")]
        public sealed partial record HeroRec(
            [Key] int Id,
            [Length(3)] [NullString("")] [ForeignKey("Skill", "Id")] ImmutableArray<int?> SkillIds,
            [SingleColumnCollection(",")] [ForeignKey("Skill", "Id")] ImmutableArray<int> PassiveIds,
            [Length(2)] [ForeignKey("Skill", "Id")] FrozenSet<int> TraitIds,
            [ForeignKey("Skill", "Id")] SkillRec.SkillId MainSkill,
            [Length(2)] [ForeignKey("Skill", "Id")] ImmutableArray<SkillRec.SkillId> ChainSkills);

        public sealed partial class HeroTable : StaticDataTable<HeroRec>;

        public partial class GenManager(ILogger logger)
            : StaticDataManager<GenManager.TableSet>(logger)
        {
            public sealed partial record TableSet(SkillTable? Skill, HeroTable? Hero);
        }
        """;

    [Fact]
    public void Array_elements_carry_their_index_and_branding_values_show_the_inner_value()
    {
        var logger = CreateLogger();

        var output = SourceGeneratorTestHelper.RunWithFinal(Source);
        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.Empty(diagnostics);

        var tree = SourceGeneratorTestHelper.GetSingleTree(output.Run, "TableSetLoader.g.cs");
        var generated = tree.ToString();
        Assert.Contains("for (var __vi = 0; __vi < __r.SkillIds.Length; __vi++)", generated, StringComparison.Ordinal);
        Assert.Contains("$\"SkillIds[{__vi}]\"", generated, StringComparison.Ordinal);
        Assert.Contains("for (var __vi = 0; __vi < __r.PassiveIds.Length; __vi++)", generated, StringComparison.Ordinal);
        Assert.Contains("$\"PassiveIds[{__vi}]\"", generated, StringComparison.Ordinal);
        Assert.Contains("foreach (var __v in __r.TraitIds)", generated, StringComparison.Ordinal);
        Assert.Contains("\"TraitIds\"", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("TraitIds[{", generated, StringComparison.Ordinal);
        Assert.Contains("$\"ChainSkills[{__vi}]\"", generated, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(generated, "$\"{__v.Value}\""));

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

    private TestOutputLogger<ForeignKeyMessageDetailEmissionTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        var created = factory.CreateLogger<ForeignKeyMessageDetailEmissionTests>();
        if (created is not TestOutputLogger<ForeignKeyMessageDetailEmissionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
