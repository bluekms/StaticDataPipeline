using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public partial class MissingCsvFileTests(ITestOutputHelper testOutputHelper)
{
    private const string SchoolCsv =
        """
        Id,Name
        1,서울대학교
        """;

    private const string StudentCsv =
        """
        Id,Name
        1,김철수
        """;

    [StaticDataRecord("School", "Sheet1")]
    private partial record SchoolRecord(int Id, string Name);

    [StaticDataRecord("Student", "Sheet1")]
    private partial record StudentRecord(int Id, string Name);

    private sealed partial class SchoolTable : StaticDataTable<SchoolRecord>;

    private sealed partial class StudentTable : StaticDataTable<StudentRecord>;

    private sealed partial class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed partial record TableSet(
            SchoolTable School,
            StudentTable? Student);
    }

    // nullable 멤버여도 CSV 가 없으면 건너뛰지 않고 실패한다. 건너뛰려면 disabledTables 에 넣어야 한다.
    [Theory]
    [InlineData("School.Sheet1.csv")]
    [InlineData("Student.Sheet1.csv")]
    public async Task Load_CsvFileMissing_FailsWithFileName(string missingFile)
    {
        var logger = CreateLogger();

        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Student.Sheet1.csv", StudentCsv);
        File.Delete(Path.Combine(dir.Path, missingFile));

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        var inner = Assert.Single(ex.InnerExceptions);
        Assert.Contains(missingFile, inner.ToString());
        Assert.Null(staticData.Current);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<MissingCsvFileTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<MissingCsvFileTests>() is not TestOutputLogger<MissingCsvFileTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
