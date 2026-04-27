using System.Collections.Immutable;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Manager;
using Sdp.Table;

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

    private record School(
        int Id,
        string Name);

    private record Teacher(
        int Id,
        string Name,
        [ForeignKey("School", "Name")] string SchoolName);

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

        public void Load()
            => Load(new TableSet(
                new(CsvLoader.Parse<School>(SchoolCsv)),
                new(CsvLoader.Parse<Teacher>(TeacherCsv)),
                new(CsvLoader.Parse<Scholarship>(ScholarshipCsv))));

        public void LoadWithErrorScholarship()
            => Load(new TableSet(
                new(CsvLoader.Parse<School>(SchoolCsv)),
                new(CsvLoader.Parse<Teacher>(TeacherCsv)),
                new(CsvLoader.Parse<Scholarship>(ErrorScholarshipCsv))));
    }

    [Fact]
    public void Load_MultipleFkColumn_ValidWhenRecipientIdExistsInOnlyOneTarget()
    {
        var staticData = new StaticData();

        staticData.Load();

        Assert.Equal(2, staticData.ScholarshipTable.Records.Count);
    }

    [Fact]
    public void Load_MultipleFkViolation_ThrowsWhenRecipientIdExistsInNeitherTarget()
    {
        var staticData = new StaticData();

        var ex = Assert.Throws<AggregateException>(staticData.LoadWithErrorScholarship);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("99", ex.InnerExceptions[0].Message);
    }
}
