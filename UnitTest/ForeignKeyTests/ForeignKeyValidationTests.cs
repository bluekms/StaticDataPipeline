using System.Collections.Immutable;
using Sdp;
using Sdp.Attributes;
using Sdp.Csv;
using Sdp.Table;

namespace UnitTest.ForeignKeyTests;

public class ForeignKeyValidationTests
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

    private const string StudentCsv =
        """
        Id,Name,SchoolId,TeacherId
        1,홍길동,1,1
        2,김영수,2,2
        3,이지은,2,2
        4,박준혁,3,3
        5,최수진,1,1
        """;

    private const string ErrorTeacherCsv =
        """
        Id,Name,SchoolName
        1,김철수,서울대학교
        2,이영희,Error대학교
        3,박민준,고려대학교
        """;

    private const string ErrorStudentCsv =
        """
        Id,Name,SchoolId,TeacherId
        1,홍길동,1,1
        2,김영수,2,2
        3,이지은,2,2
        4,박준혁,999,3
        5,최수진,1,1
        """;

    private record School(
        int Id,
        string Name);

    private record Teacher(
        int Id,
        string Name,
        [ForeignKey("School", "Name")] string SchoolName);

    private record Student(
        int Id,
        string Name,
        [ForeignKey("School", "Id")] int SchoolId,
        [ForeignKey("Teacher", "Id")] int TeacherId);

    private sealed class SchoolTable : StaticDataTable<School, int>
    {
        public SchoolTable(ImmutableList<School> records)
            : base(records, x => x.Id, "Id")
        {
        }
    }

    private sealed class TeacherTable : StaticDataTable<Teacher, int>
    {
        public TeacherTable(ImmutableList<Teacher> records)
            : base(records, x => x.Id, "Id")
        {
        }
    }

    private sealed class StudentTable : StaticDataTable<Student, int>
    {
        public StudentTable(ImmutableList<Student> records)
            : base(records, x => x.Id, "Id")
        {
        }
    }

    private sealed class StaticData : StaticDataManager<StaticData.TableSet>
    {
        public sealed record TableSet(
            SchoolTable? School,
            TeacherTable? Teacher,
            StudentTable? Student);

        public SchoolTable SchoolTable => Current.School!;
        public TeacherTable TeacherTable => Current.Teacher!;
        public StudentTable StudentTable => Current.Student!;

        public void Load()
            => Load(new TableSet(
                new(CsvLoader.Parse<School>(SchoolCsv)),
                new(CsvLoader.Parse<Teacher>(TeacherCsv)),
                new(CsvLoader.Parse<Student>(StudentCsv))));

        public void LoadWithErrorTeacher()
            => Load(new TableSet(
                new(CsvLoader.Parse<School>(SchoolCsv)),
                new(CsvLoader.Parse<Teacher>(ErrorTeacherCsv)),
                new(CsvLoader.Parse<Student>(StudentCsv))));

        public void LoadWithErrorStudent()
            => Load(new TableSet(
                new(CsvLoader.Parse<School>(SchoolCsv)),
                new(CsvLoader.Parse<Teacher>(TeacherCsv)),
                new(CsvLoader.Parse<Student>(ErrorStudentCsv))));

        public void Load(SchoolTable school, TeacherTable teacher, StudentTable? student = null)
            => Load(new TableSet(school, teacher, student));
    }

    [Fact]
    public void Load_ValidData_SucceedsWithoutException()
    {
        var staticData = new StaticData();

        staticData.Load();

        Assert.Equal(5, staticData.SchoolTable.Records.Count);
        Assert.Equal(3, staticData.TeacherTable.Records.Count);
        Assert.Equal(5, staticData.StudentTable.Records.Count);
    }

    [Fact]
    public void Load_KeyFk_StudentSchoolIdResolvesToSchool()
    {
        var staticData = new StaticData();
        staticData.Load();

        var student = staticData.StudentTable.Get(4);
        Assert.Equal(3, student.SchoolId);

        var school = staticData.SchoolTable.Get(student.SchoolId);
        Assert.Equal("고려대학교", school.Name);
    }

    [Fact]
    public void Load_KeyFk_StudentTeacherIdResolvesToTeacher()
    {
        var staticData = new StaticData();
        staticData.Load();

        var student = staticData.StudentTable.Get(3);
        Assert.Equal(2, student.TeacherId);

        var teacher = staticData.TeacherTable.Get(student.TeacherId);
        Assert.Equal("이영희", teacher.Name);
    }

    [Fact]
    public void Load_NonKeyFkViolation_ThrowsAggregateException()
    {
        var staticData = new StaticData();

        var ex = Assert.Throws<AggregateException>(staticData.LoadWithErrorTeacher);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("Error대학교", ex.InnerExceptions[0].Message);
    }

    [Fact]
    public void Load_KeyFkViolation_ThrowsAggregateException()
    {
        var staticData = new StaticData();

        var ex = Assert.Throws<AggregateException>(staticData.LoadWithErrorStudent);

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", ex.InnerExceptions[0].Message);
    }
}
