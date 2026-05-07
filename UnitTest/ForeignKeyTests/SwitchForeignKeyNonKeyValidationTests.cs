using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;

namespace UnitTest.ForeignKeyTests;

public class SwitchForeignKeyNonKeyValidationTests
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
    private record Staff(
        int Id,
        Department Department,
        [SwitchForeignKey("Department", "Engineering", "Engineer", "Name")]
        [SwitchForeignKey("Department", "Design",      "Designer", "Name")]
        string LeadName);

    [StaticDataRecord("Engineer", "Sheet1")]
    private record Engineer(int Id, string Name);

    [StaticDataRecord("Designer", "Sheet1")]
    private record Designer(int Id, string Name);

    private sealed class StaffTable(ImmutableList<Staff> records)
        : StaticDataTable<StaffTable, Staff>(records);

    private sealed class EngineerTable(ImmutableList<Engineer> records)
        : StaticDataTable<EngineerTable, Engineer>(records);

    private sealed class DesignerTable(ImmutableList<Designer> records)
        : StaticDataTable<DesignerTable, Designer>(records);

    private sealed class StaticData : StaticDataManager<StaticData.TableSet>
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
        using var dir = new CsvTestDirectory();
        dir.Write("Staff.Sheet1.csv", StaffCsv);
        dir.Write("Engineer.Sheet1.csv", EngineerCsv);
        dir.Write("Designer.Sheet1.csv", DesignerCsv);

        var staticData = new StaticData();
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.StaffTable.Records.Count);
    }

    [Fact]
    public async Task Load_NonKeySwitchFkViolation_ThrowsAggregateException()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("Staff.Sheet1.csv", ErrorStaffCsv);
        dir.Write("Engineer.Sheet1.csv", EngineerCsv);
        dir.Write("Designer.Sheet1.csv", DesignerCsv);

        var staticData = new StaticData();

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("UnknownDesigner", ex.InnerExceptions[0].Message);
    }
}
