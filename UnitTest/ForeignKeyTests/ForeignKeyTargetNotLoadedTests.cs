using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public partial class ForeignKeyTargetNotLoadedTests(ITestOutputHelper testOutputHelper)
{
    private const string SchoolCsv =
        """
        Id,Name
        1,서울대학교
        """;

    private const string StudentCsv =
        """
        Id,SchoolId
        1,1
        2,1
        """;

    private const string EmptyStudentCsv =
        """
        Id,SchoolId
        """;

    private const string AllNullOptionalStudentCsv =
        """
        Id,SchoolId
        1,
        2,
        """;

    [StaticDataRecord("School", "Sheet1")]
    private partial record SchoolRecord(int Id, string Name);

    [StaticDataRecord("Student", "Sheet1")]
    private partial record StudentRecord(
        int Id,
        [ForeignKey("School", "Id")] int SchoolId);

    [StaticDataRecord("OptionalStudent", "Sheet1")]
    private partial record OptionalStudentRecord(
        int Id,
        [ForeignKey("School", "Id")] [NullString("")] int? SchoolId);

    private sealed partial class SchoolTable : StaticDataTable<SchoolRecord>;

    private sealed partial class StudentTable : StaticDataTable<StudentRecord>;

    private sealed partial class OptionalStudentTable : StaticDataTable<OptionalStudentRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            SchoolTable? School,
            StudentTable? Student,
            OptionalStudentTable? OptionalStudent);
    }

    [Fact]
    public async Task Load_TargetDisabled_WithValuesToVerify_ThrowsTargetNotLoadedOnce()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Student.Sheet1.csv", StudentCsv);
        dir.Write("OptionalStudent.Sheet1.csv", EmptyStudentCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => staticData.LoadAsync(dir.Path, new List<string> { "School" }));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains("School", inner.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TargetDisabled_WithoutRows_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Student.Sheet1.csv", EmptyStudentCsv);
        dir.Write("OptionalStudent.Sheet1.csv", EmptyStudentCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path, new List<string> { "School" });

        Assert.Null(staticData.Current.School);
        Assert.Empty(staticData.Current.Student!.Records);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TargetDisabled_AllNullReferences_Succeeds()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("Student.Sheet1.csv", EmptyStudentCsv);
        dir.Write("OptionalStudent.Sheet1.csv", AllNullOptionalStudentCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path, new List<string> { "School" });

        Assert.Equal(2, staticData.Current.OptionalStudent!.Records.Length);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_TargetLoaded_ValuesAreVerified()
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Student.Sheet1.csv", StudentCsv);
        dir.Write("OptionalStudent.Sheet1.csv", AllNullOptionalStudentCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.Current.Student!.Records.Length);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<ForeignKeyTargetNotLoadedTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyTargetNotLoadedTests>() is not TestOutputLogger<ForeignKeyTargetNotLoadedTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
