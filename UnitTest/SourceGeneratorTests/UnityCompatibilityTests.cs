using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.SourceGeneratorTests;

// 유니티 소비자 컴파일레이션은 C# 9 수준이다. 생성 코드(매퍼·테이블 팩토리·TableSet 로더·
// 매니저 브리지·View 팩토리·ViewSet 빌더)가 C# 9 문법만 사용하는지를 컴파일 게이트로 검증한다.
public class UnityCompatibilityTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Generated_code_compiles_under_csharp9()
    {
        var logger = CreateLogger();

        // 소비자 코드도 C# 9 문법으로 작성한다 (block namespace, primary constructor 미사용).
        // language=C#
        const string source = """
            using System.Collections.Immutable;
            using Microsoft.Extensions.Logging;
            using Sdp.Attributes;
            using Sdp.Manager;
            using Sdp.Table;
            using Sdp.View;

            namespace Test
            {
                public sealed partial record Address(string City, string Detail);

                [StaticDataRecord("File", "Events")]
                public sealed partial record EventRecord([Key] int Id, string Name, Address Where, ImmutableArray<int> Tags);

                [StaticDataRecord("File", "Items")]
                public sealed partial record ItemRecord(
                    [Key] int Id,
                    [ForeignKey("Events", "Id")] int EventId,
                    string Name);

                public sealed partial class EventTable : StaticDataTable<EventTable, EventRecord>
                {
                }

                public sealed partial class ItemTable : StaticDataTable<ItemTable, ItemRecord>
                {
                }

                public sealed partial class EventView : StaticDataView<EventView, GameStaticData.TableSet>
                {
                    public EventView(GameStaticData.TableSet tables)
                        : base(tables)
                    {
                    }
                }

                public sealed partial class GameStaticData : StaticDataManager<GameStaticData.TableSet, GameStaticData.ViewSet>
                {
                    public GameStaticData(ILogger logger)
                        : base(logger)
                    {
                    }

                    public sealed partial record TableSet(EventTable? Events, ItemTable? Items);

                    public sealed partial record ViewSet(EventView Event);
                }
            }
            """;

        var output = SourceGeneratorTestHelper.RunWithFinal(source, LanguageVersion.CSharp9);

        var diagnostics = output.Run.Results.SelectMany(r => r.Diagnostics).ToList();
        Assert.DoesNotContain(diagnostics, d => d.Id.StartsWith("SDP", StringComparison.Ordinal));

        var errors = SourceGeneratorTestHelper.GetCompilationErrors(output.Final);
        foreach (var error in errors)
        {
            logger.LogInformation(
                "Error {Id} at {Path}: {Message}",
                error.Id,
                error.Location.SourceTree?.FilePath,
                error.GetMessage(CultureInfo.InvariantCulture));
        }

        Assert.Empty(errors);

        logger.LogInformation(
            "Generated trees compiled under C# 9: [{Files}]",
            string.Join(", ", output.Run.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath))));
    }

    private TestOutputLogger<UnityCompatibilityTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<UnityCompatibilityTests>()
            is not TestOutputLogger<UnityCompatibilityTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
