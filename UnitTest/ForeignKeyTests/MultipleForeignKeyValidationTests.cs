using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;

namespace UnitTest.ForeignKeyTests;

public class MultipleForeignKeyValidationTests
{
    private const string SchoolCsv =
        """
        Id,Name
        1,서울대학교
        2,연세대학교
        3,고려대학교
        4,한양대학교
        5,부산대학교
        """;

    private const string TeacherCsv =
        """
        Id,Name,SchoolName
        1,김철수,서울대학교
        2,이영희,연세대학교
        3,박민준,고려대학교
        """;

    private const string ScholarshipCsv =
        """
        Id,Title,RecipientId
        1,우수 장학금,1
        2,특별 장학금,4
        """;

    private const string ErrorScholarshipCsv =
        """
        Id,Title,RecipientId
        1,우수 장학금,1
        2,오류 장학금,99
        """;

    [StaticDataRecord("School", "Sheet1")]
    private record School(
        int Id,
        string Name);

    [StaticDataRecord("Teacher", "Sheet1")]
    private record Teacher(
        int Id,
        string Name,
        [ForeignKey("School", "Name")] string SchoolName);

    [StaticDataRecord("Scholarship", "Sheet1")]
    private record Scholarship(
        int Id,
        string Title,
        [ForeignKey("School", "Id")]
        [ForeignKey("Teacher", "Id")]
        int RecipientId);

    private sealed class SchoolTable(ImmutableList<School> records)
        : StaticDataTable<SchoolTable, School>(records);

    private sealed class TeacherTable(ImmutableList<Teacher> records)
        : StaticDataTable<TeacherTable, Teacher>(records);

    private sealed class ScholarshipTable(ImmutableList<Scholarship> records)
        : StaticDataTable<ScholarshipTable, Scholarship>(records);

    private sealed class StaticData : StaticDataManager<StaticData.TableSet>
    {
        public sealed record TableSet(
            SchoolTable? School,
            TeacherTable? Teacher,
            ScholarshipTable? Scholarship);

        public SchoolTable SchoolTable => Current.School!;
        public TeacherTable TeacherTable => Current.Teacher!;
        public ScholarshipTable ScholarshipTable => Current.Scholarship!;
    }

    [Fact]
    public async Task Load_MultipleFkColumn_ValidWhenRecipientIdExistsInOnlyOneTarget()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Teacher.Sheet1.csv", TeacherCsv);
        dir.Write("Scholarship.Sheet1.csv", ScholarshipCsv);

        var staticData = new StaticData();
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(2, staticData.ScholarshipTable.Records.Count);
    }

    [Fact]
    public async Task Load_MultipleFkViolation_ThrowsWhenRecipientIdExistsInNeitherTarget()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Teacher.Sheet1.csv", TeacherCsv);
        dir.Write("Scholarship.Sheet1.csv", ErrorScholarshipCsv);

        var staticData = new StaticData();

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("99", ex.InnerExceptions[0].Message);
    }
}
