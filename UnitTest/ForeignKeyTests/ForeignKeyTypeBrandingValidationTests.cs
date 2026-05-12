using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public class ForeignKeyTypeBrandingValidationTests(ITestOutputHelper testOutputHelper)
{
    private TestOutputLogger<ForeignKeyTypeBrandingValidationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyTypeBrandingValidationTests>()
            is not TestOutputLogger<ForeignKeyTypeBrandingValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }

    // 1) enum 브랜딩 — FK 가 같은 enum 타입을 양쪽에서 인식하는가
    private enum HeroId
    {
        Knight = 1,
        Wizard = 2,
        Archer = 3,
        Paladin = 4,
    }

    [StaticDataRecord("Hero", "Sheet1")]
    private record HeroRecord([Key] HeroId Id, string Name);

    [StaticDataRecord("HeroQuest", "Sheet1")]
    private record HeroQuestRecord(
        int QuestId,
        [ForeignKey("Hero", "Id")] HeroId AssignedTo);

    private sealed class HeroTable(ImmutableList<HeroRecord> records)
        : StaticDataTable<HeroTable, HeroRecord>(records);

    private sealed class HeroQuestTable(ImmutableList<HeroQuestRecord> records)
        : StaticDataTable<HeroQuestTable, HeroQuestRecord>(records);

    private sealed class EnumBrandedStaticData(ILogger logger)
        : StaticDataManager<EnumBrandedStaticData.TableSet>(logger)
    {
        public sealed record TableSet(HeroTable? Hero, HeroQuestTable? HeroQuest);

        public HeroTable HeroTable => Current.Hero!;

        public HeroQuestTable HeroQuestTable => Current.HeroQuest!;
    }

    private const string HeroCsv =
        """
        Id,Name
        Knight,기사
        Wizard,마법사
        Archer,궁수
        """;

    private const string ValidHeroQuestCsv =
        """
        QuestId,AssignedTo
        100,Knight
        101,Wizard
        102,Archer
        """;

    // Paladin 은 enum 멤버이긴 하지만 Hero 테이블에는 없음 → FK 위반
    private const string MissingHeroQuestCsv =
        """
        QuestId,AssignedTo
        100,Knight
        101,Paladin
        """;

    [Fact]
    public async Task Load_EnumBrandedFk_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Hero.Sheet1.csv", HeroCsv);
        dir.Write("HeroQuest.Sheet1.csv", ValidHeroQuestCsv);

        var staticData = new EnumBrandedStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(3, staticData.HeroTable.Records.Count);
        Assert.Equal(3, staticData.HeroQuestTable.Records.Count);

        var quest = staticData.HeroQuestTable.Records.First(q => q.QuestId == 100);
        Assert.Equal(HeroId.Knight, quest.AssignedTo);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_EnumBrandedFkViolation_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Hero.Sheet1.csv", HeroCsv);
        dir.Write("HeroQuest.Sheet1.csv", MissingHeroQuestCsv);

        var staticData = new EnumBrandedStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("Paladin", ex.InnerExceptions[0].Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    // 2) single-parameter record 브랜딩 — FK 가 같은 record 타입을 양쪽에서 인식하는가
    private sealed record CharId(int Value);

    [StaticDataRecord("Character", "Sheet1")]
    private record CharacterRecord([Key] CharId Id, string Name);

    [StaticDataRecord("CharacterQuest", "Sheet1")]
    private record CharacterQuestRecord(
        int QuestId,
        [ForeignKey("Character", "Id")] CharId AssignedTo);

    private sealed class CharacterTable(ImmutableList<CharacterRecord> records)
        : StaticDataTable<CharacterTable, CharacterRecord>(records);

    private sealed class CharacterQuestTable(ImmutableList<CharacterQuestRecord> records)
        : StaticDataTable<CharacterQuestTable, CharacterQuestRecord>(records);

    private sealed class RecordBrandedStaticData(ILogger logger)
        : StaticDataManager<RecordBrandedStaticData.TableSet>(logger)
    {
        public sealed record TableSet(CharacterTable? Character, CharacterQuestTable? CharacterQuest);

        public CharacterTable CharacterTable => Current.Character!;

        public CharacterQuestTable CharacterQuestTable => Current.CharacterQuest!;
    }

    private const string CharacterCsv =
        """
        Id,Name
        100,Aria
        200,Bren
        300,Cynan
        """;

    private const string ValidCharacterQuestCsv =
        """
        QuestId,AssignedTo
        1,100
        2,200
        3,300
        """;

    // 999 는 Character 테이블에 없음 → FK 위반
    private const string MissingCharacterQuestCsv =
        """
        QuestId,AssignedTo
        1,100
        2,999
        """;

    [Fact]
    public async Task Load_RecordBrandedFk_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Character.Sheet1.csv", CharacterCsv);
        dir.Write("CharacterQuest.Sheet1.csv", ValidCharacterQuestCsv);

        var staticData = new RecordBrandedStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(3, staticData.CharacterTable.Records.Count);
        Assert.Equal(3, staticData.CharacterQuestTable.Records.Count);

        var quest = staticData.CharacterQuestTable.Records.First(q => q.QuestId == 1);
        Assert.Equal(new CharId(100), quest.AssignedTo);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_RecordBrandedFkViolation_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Character.Sheet1.csv", CharacterCsv);
        dir.Write("CharacterQuest.Sheet1.csv", MissingCharacterQuestCsv);

        var staticData = new RecordBrandedStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", ex.InnerExceptions[0].Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    // 3) SFK + enum 브랜딩 — 분기마다 다른 테이블, 같은 enum 키 타입
    private enum FaceId
    {
        Alpha = 1,
        Beta = 2,
        Gamma = 3,
        Omega = 99,
    }

    private enum FaceCategory
    {
        Npc,
        Pc,
    }

    [StaticDataRecord("NpcFace", "Sheet1")]
    private record NpcFaceRecord([Key] FaceId Id, string Description);

    [StaticDataRecord("PcFace", "Sheet1")]
    private record PcFaceRecord([Key] FaceId Id, string Description);

    [StaticDataRecord("FaceLookup", "Sheet1")]
    private record FaceLookupRecord(
        int LookupId,
        FaceCategory Category,
        [SwitchForeignKey("Category", "Npc", "NpcFace", "Id")]
        [SwitchForeignKey("Category", "Pc", "PcFace", "Id")]
        FaceId FaceId);

    private sealed class NpcFaceTable(ImmutableList<NpcFaceRecord> records)
        : StaticDataTable<NpcFaceTable, NpcFaceRecord>(records);

    private sealed class PcFaceTable(ImmutableList<PcFaceRecord> records)
        : StaticDataTable<PcFaceTable, PcFaceRecord>(records);

    private sealed class FaceLookupTable(ImmutableList<FaceLookupRecord> records)
        : StaticDataTable<FaceLookupTable, FaceLookupRecord>(records);

    private sealed class EnumBrandedSfkStaticData(ILogger logger)
        : StaticDataManager<EnumBrandedSfkStaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            NpcFaceTable? NpcFace,
            PcFaceTable? PcFace,
            FaceLookupTable? FaceLookup);

        public FaceLookupTable FaceLookupTable => Current.FaceLookup!;
    }

    private const string NpcFaceCsv =
        """
        Id,Description
        Alpha,눈썹
        Beta,수염
        """;

    private const string PcFaceCsv =
        """
        Id,Description
        Beta,짧은머리
        Gamma,긴머리
        """;

    private const string ValidFaceLookupCsv =
        """
        LookupId,Category,FaceId
        10,Npc,Alpha
        11,Npc,Beta
        12,Pc,Beta
        13,Pc,Gamma
        """;

    // Omega 는 enum 멤버지만 NpcFace / PcFace 어느 테이블에도 없음
    private const string MissingFaceLookupCsv =
        """
        LookupId,Category,FaceId
        10,Npc,Alpha
        11,Npc,Omega
        """;

    [Fact]
    public async Task Load_EnumBrandedSfk_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("NpcFace.Sheet1.csv", NpcFaceCsv);
        dir.Write("PcFace.Sheet1.csv", PcFaceCsv);
        dir.Write("FaceLookup.Sheet1.csv", ValidFaceLookupCsv);

        var staticData = new EnumBrandedSfkStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(4, staticData.FaceLookupTable.Records.Count);

        var pcEntry = staticData.FaceLookupTable.Records.First(r => r.LookupId == 13);
        Assert.Equal(FaceCategory.Pc, pcEntry.Category);
        Assert.Equal(FaceId.Gamma, pcEntry.FaceId);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_EnumBrandedSfkViolation_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("NpcFace.Sheet1.csv", NpcFaceCsv);
        dir.Write("PcFace.Sheet1.csv", PcFaceCsv);
        dir.Write("FaceLookup.Sheet1.csv", MissingFaceLookupCsv);

        var staticData = new EnumBrandedSfkStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("Omega", ex.InnerExceptions[0].Message, StringComparison.Ordinal);
        Assert.Contains("when Category=Npc", ex.InnerExceptions[0].Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }

    // 4) SFK + single-parameter record 브랜딩 — 분기마다 다른 테이블, 같은 record 키 타입
    private sealed record PartId(int Value);

    private enum PartKind
    {
        Metal,
        Wood,
    }

    [StaticDataRecord("MetalPart", "Sheet1")]
    private record MetalPartRecord([Key] PartId Id, string Name);

    [StaticDataRecord("WoodPart", "Sheet1")]
    private record WoodPartRecord([Key] PartId Id, string Name);

    [StaticDataRecord("PartLookup", "Sheet1")]
    private record PartLookupRecord(
        int LookupId,
        PartKind Kind,
        [SwitchForeignKey("Kind", "Metal", "MetalPart", "Id")]
        [SwitchForeignKey("Kind", "Wood", "WoodPart", "Id")]
        PartId PartId);

    private sealed class MetalPartTable(ImmutableList<MetalPartRecord> records)
        : StaticDataTable<MetalPartTable, MetalPartRecord>(records);

    private sealed class WoodPartTable(ImmutableList<WoodPartRecord> records)
        : StaticDataTable<WoodPartTable, WoodPartRecord>(records);

    private sealed class PartLookupTable(ImmutableList<PartLookupRecord> records)
        : StaticDataTable<PartLookupTable, PartLookupRecord>(records);

    private sealed class RecordBrandedSfkStaticData(ILogger logger)
        : StaticDataManager<RecordBrandedSfkStaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            MetalPartTable? MetalPart,
            WoodPartTable? WoodPart,
            PartLookupTable? PartLookup);

        public PartLookupTable PartLookupTable => Current.PartLookup!;
    }

    private const string MetalPartCsv =
        """
        Id,Name
        10,Iron
        20,Steel
        """;

    private const string WoodPartCsv =
        """
        Id,Name
        30,Oak
        40,Pine
        """;

    private const string ValidPartLookupCsv =
        """
        LookupId,Kind,PartId
        1,Metal,10
        2,Metal,20
        3,Wood,30
        4,Wood,40
        """;

    // 999 는 MetalPart / WoodPart 어느 테이블에도 없음
    private const string MissingPartLookupCsv =
        """
        LookupId,Kind,PartId
        1,Metal,10
        2,Wood,999
        """;

    [Fact]
    public async Task Load_RecordBrandedSfk_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("MetalPart.Sheet1.csv", MetalPartCsv);
        dir.Write("WoodPart.Sheet1.csv", WoodPartCsv);
        dir.Write("PartLookup.Sheet1.csv", ValidPartLookupCsv);

        var staticData = new RecordBrandedSfkStaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(4, staticData.PartLookupTable.Records.Count);

        var woodEntry = staticData.PartLookupTable.Records.First(r => r.LookupId == 4);
        Assert.Equal(PartKind.Wood, woodEntry.Kind);
        Assert.Equal(new PartId(40), woodEntry.PartId);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_RecordBrandedSfkViolation_ThrowsAggregateException()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("MetalPart.Sheet1.csv", MetalPartCsv);
        dir.Write("WoodPart.Sheet1.csv", WoodPartCsv);
        dir.Write("PartLookup.Sheet1.csv", MissingPartLookupCsv);

        var staticData = new RecordBrandedSfkStaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", ex.InnerExceptions[0].Message, StringComparison.Ordinal);
        Assert.Contains("when Kind=Wood", ex.InnerExceptions[0].Message, StringComparison.Ordinal);
        Assert.Empty(logger.Logs);
    }
}
