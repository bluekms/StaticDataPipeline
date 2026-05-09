using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public class SwitchForeignKeyNonKeyValidationTests(ITestOutputHelper testOutputHelper)
{
    private enum Department
    {
        Engineering,
        Design,
    }

    private const string StaffCsv =
        """
        Id,Department,LeadName
        1,Engineering,Alice
        2,Design,Bob
        """;

    private const string ErrorStaffCsv =
        """
        Id,Department,LeadName
        1,Engineering,Alice
        2,Design,UnknownDesigner
        """;

    private const string EngineerCsv =
        """
        Id,Name
        1,Alice
        2,Charlie
        """;

    private const string DesignerCsv =
        """
        Id,Name
        1,Bob
        2,Diana
        """;

    [StaticDataRecord("Staff", "Sheet1")]
    private record StaffRecord(
        int Id,
        Department Department,
        [SwitchForeignKey("Department", "Engineering", "Engineer", "Name")]
        [SwitchForeignKey("Department", "Design",      "Designer", "Name")]
        string LeadName);

    [StaticDataRecord("Engineer", "Sheet1")]
    private record EngineerRecord(int Id, string Name);

    [StaticDataRecord("Designer", "Sheet1")]
    private record DesignerRecord(int Id, string Name);

    private sealed class StaffTable(ImmutableList<StaffRecord> records)
        : StaticDataTable<StaffTable, StaffRecord>(records);

    private sealed class EngineerTable(ImmutableList<EngineerRecord> records)
        : StaticDataTable<EngineerTable, EngineerRecord>(records);

    private sealed class DesignerTable(ImmutableList<DesignerRecord> records)
        : StaticDataTable<DesignerTable, DesignerRecord>(records);

    private sealed class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            StaffTable? Staff,
            EngineerTable? Engineer,
            DesignerTable? Designer);

        public StaffTable StaffTable => Current.Staff!;
    }

    [Fact]
    public async Task Load_NonKeySwitchFk_ValidData_SucceedsWithoutException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyNonKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyNonKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Staff.Sheet1.csv", StaffCsv);
        dir.Write("Engineer.Sheet1.csv", EngineerCsv);
        dir.Write("Designer.Sheet1.csv", DesignerCsv);

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.StaffTable.Records.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_NonKeySwitchFkViolation_ThrowsAggregateException()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<SwitchForeignKeyNonKeyValidationTests>() is not TestOutputLogger<SwitchForeignKeyNonKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        using var dir = new CsvTestDirectory();
        dir.Write("Staff.Sheet1.csv", ErrorStaffCsv);
        dir.Write("Engineer.Sheet1.csv", EngineerCsv);
        dir.Write("Designer.Sheet1.csv", DesignerCsv);

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("UnknownDesigner", ex.InnerExceptions[0].Message);
        Assert.Empty(logger.Logs);
    }
}
