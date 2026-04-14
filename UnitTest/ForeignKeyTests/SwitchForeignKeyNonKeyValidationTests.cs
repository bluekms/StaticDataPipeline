using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Manager;
using Sdp.Table;

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

    private record Staff(
        int Id,
        Department Department,
        [SwitchForeignKey("Department", "Engineering", "Engineer", "Name")]
        [SwitchForeignKey("Department", "Design",      "Designer", "Name")]
        string LeadName);

    private record Engineer(int Id, string Name);
    private record Designer(int Id, string Name);

    private sealed class StaffTable : StaticDataTable<StaffTable, Staff, int>
    {
        public StaffTable(ImmutableList<Staff> records)
            : base(records, x => x.Id, "Id")
        {
        }
    }

    private sealed class EngineerTable : StaticDataTable<EngineerTable, Engineer, int>
    {
        public EngineerTable(ImmutableList<Engineer> records)
            : base(records, x => x.Id, "Id")
        {
        }
    }

    private sealed class DesignerTable : StaticDataTable<DesignerTable, Designer, int>
    {
        public DesignerTable(ImmutableList<Designer> records)
            : base(records, x => x.Id, "Id")
        {
        }
    }

    private sealed class StaticData : StaticDataManager<StaticData.TableSet>
    {
        public sealed record TableSet(
            StaffTable? Staff,
            EngineerTable? Engineer,
            DesignerTable? Designer);

        public StaffTable StaffTable => Current.Staff!;

        public void Load()
            => Load(new TableSet(
                new(CsvLoader.Parse<Staff>(StaffCsv)),
                new(CsvLoader.Parse<Engineer>(EngineerCsv)),
                new(CsvLoader.Parse<Designer>(DesignerCsv))));

        public void LoadWithErrorStaff()
            => Load(new TableSet(
                new(CsvLoader.Parse<Staff>(ErrorStaffCsv)),
                new(CsvLoader.Parse<Engineer>(EngineerCsv)),
                new(CsvLoader.Parse<Designer>(DesignerCsv))));
    }

    [Fact]
    public void Load_NonKeySwitchFk_ValidData_SucceedsWithoutException()
    {
        var staticData = new StaticData();

        staticData.Load();

        Assert.Equal(2, staticData.StaffTable.Records.Count);
    }

    [Fact]
    public void Load_NonKeySwitchFkViolation_ThrowsAggregateException()
    {
        var staticData = new StaticData();

        var ex = Assert.Throws<AggregateException>(staticData.LoadWithErrorStaff);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("UnknownDesigner", ex.InnerExceptions[0].Message);
    }
}
