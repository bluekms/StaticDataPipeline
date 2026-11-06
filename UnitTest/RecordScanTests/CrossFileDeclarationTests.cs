using System.Data;
using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Catalogs;
using SchemaInfoScanner.Collectors;
using SchemaInfoScanner.Schemata.TypedPropertySchemata;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.RecordScanTests;

public class CrossFileDeclarationTests(ITestOutputHelper testOutputHelper)
{
    // language=C#
    private const string GradeSource = """
        namespace Game.Records;

        public enum Grade
        {
            Bronze,
            Silver,
            Gold,
        }
        """;

    // language=C#
    private const string ItemSource = """
        using Sdp.Attributes;

        namespace Game.Records;

        public sealed partial record ItemRecord([Key] ItemRecord.ItemId Id, string Name)
        {
            public readonly record struct ItemId(int Value);
        }
        """;

    // language=C#
    private const string PlayerSource = """
        using Sdp.Attributes;

        namespace Game.Records;

        [StaticDataRecord("Game", "Players")]
        public sealed partial record PlayerRecord([Key] int Id, Grade Grade, ItemRecord.ItemId Weapon);
        """;

    [Fact]
    public void EnumAndNestedRecordInOtherFiles_AreResolved()
    {
        var logger = CreateLogger();
        using var directory = WriteSources();

        var results = RecordSchemaLoader.Load(directory.Path, logger);
        var recordSchemaCatalog = CreateCatalog(results, logger);

        var player = Assert.Single(recordSchemaCatalog.StaticDataRecordSchemata);
        var headers = RecordFlattener.Flatten(player, recordSchemaCatalog, logger);
        Assert.Equal(3, results.Count);
        Assert.Equal(["Id", "Grade", "Weapon"], headers);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void EnumInOtherFile_CellValueIsCheckedAgainstItsMembers()
    {
        var logger = CreateLogger();
        using var directory = WriteSources();

        var results = RecordSchemaLoader.Load(directory.Path, logger);
        var recordSchemaCatalog = CreateCatalog(results, logger);
        var catalogs = new MetadataCatalogs(recordSchemaCatalog);
        var player = Assert.Single(recordSchemaCatalog.StaticDataRecordSchemata);
        var gradeSchema = player.PropertySchemata.Single(x => x.PropertyName.Name == "Grade");

        var accepted = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("B2", "Gold")]);
        gradeSchema.CheckCompatibility(accepted);

        var rejected = CompatibilityContext.CreateNoCollect(catalogs, [new CellData("B3", "Platinum")]);
        Assert.Throws<InvalidOperationException>(() => gradeSchema.CheckCompatibility(rejected));
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SameTypeDeclaredInTwoFiles_FailsToLoad()
    {
        var logger = CreateLogger();
        using var directory = new CsvTestDirectory();
        directory.Write("Grade.cs", GradeSource);
        directory.Write("GradeCopy.cs", GradeSource);

        Assert.Throws<SyntaxErrorException>(() => RecordSchemaLoader.Load(directory.Path, logger));
        Assert.Contains(logger.Logs, x => x.Message.Contains("CS0101", StringComparison.Ordinal));
    }

    private static CsvTestDirectory WriteSources()
    {
        var directory = new CsvTestDirectory();
        directory.Write("Grade.cs", GradeSource);
        directory.Write("Item.cs", ItemSource);
        directory.Write("Player.cs", PlayerSource);

        return directory;
    }

    private static RecordSchemaCatalog CreateCatalog(IReadOnlyList<RecordSchemaLoader.Result> results, ILogger logger)
    {
        var recordSchemaSet = new RecordSchemaSet(results, logger);
        var recordSchemaCatalog = new RecordSchemaCatalog(recordSchemaSet);
        RecordComplianceChecker.Check(recordSchemaCatalog, logger);

        return recordSchemaCatalog;
    }

    private TestOutputLogger<CrossFileDeclarationTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<CrossFileDeclarationTests>() is not TestOutputLogger<CrossFileDeclarationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
