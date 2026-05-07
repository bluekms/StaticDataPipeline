using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Manager;
using Sdp.Table;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.ForeignKeyTests;

public class ForeignKeyValidationTests(ITestOutputHelper testOutputHelper)
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

    [StaticDataRecord("School", "Sheet1")]
    private record School(
        int Id,
        string Name);

    [StaticDataRecord("Teacher", "Sheet1")]
    private record Teacher(
        int Id,
        string Name,
        [ForeignKey("School", "Name")] string SchoolName);

    [StaticDataRecord("Student", "Sheet1")]
    private record Student(
        int Id,
        string Name,
        [ForeignKey("School", "Id")] int SchoolId,
        [ForeignKey("Teacher", "Id")] int TeacherId);

    private sealed class SchoolTable : StaticDataTable<SchoolTable, School>
    {
        private readonly UniqueIndex<School, int> byId;

        public SchoolTable(ImmutableList<School> records)
            : base(records)
        {
            byId = new(records, x => x.Id);
        }

        public School Get(int id) => byId.Get(id);
    }

    private sealed class TeacherTable : StaticDataTable<TeacherTable, Teacher>
    {
        private readonly UniqueIndex<Teacher, int> byId;

        public TeacherTable(ImmutableList<Teacher> records)
            : base(records)
        {
            byId = new(records, x => x.Id);
        }

        public Teacher Get(int id) => byId.Get(id);
    }

    private sealed class StudentTable : StaticDataTable<StudentTable, Student>
    {
        private readonly UniqueIndex<Student, int> byId;

        public StudentTable(ImmutableList<Student> records)
            : base(records)
        {
            byId = new(records, x => x.Id);
        }

        public Student Get(int id) => byId.Get(id);
    }

    private sealed class StaticData(ILogger logger)
        : StaticDataManager<StaticData.TableSet>(logger)
    {
        public sealed record TableSet(
            SchoolTable? School,
            TeacherTable? Teacher,
            StudentTable? Student);

        public SchoolTable SchoolTable => Current.School!;
        public TeacherTable TeacherTable => Current.Teacher!;
        public StudentTable StudentTable => Current.Student!;
    }

    [Fact]
    public async Task Load_ValidData_SucceedsWithoutException()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Teacher.Sheet1.csv", TeacherCsv);
        dir.Write("Student.Sheet1.csv", StudentCsv);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyValidationTests>() is not TestOutputLogger<ForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        Assert.Equal(5, staticData.SchoolTable.Records.Count);
        Assert.Equal(3, staticData.TeacherTable.Records.Count);
        Assert.Equal(5, staticData.StudentTable.Records.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_KeyFk_StudentSchoolIdResolvesToSchool()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Teacher.Sheet1.csv", TeacherCsv);
        dir.Write("Student.Sheet1.csv", StudentCsv);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyValidationTests>() is not TestOutputLogger<ForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var student = staticData.StudentTable.Get(4);
        Assert.Equal(3, student.SchoolId);

        var school = staticData.SchoolTable.Get(student.SchoolId);
        Assert.Equal("고려대학교", school.Name);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_KeyFk_StudentTeacherIdResolvesToTeacher()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Teacher.Sheet1.csv", TeacherCsv);
        dir.Write("Student.Sheet1.csv", StudentCsv);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyValidationTests>() is not TestOutputLogger<ForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var staticData = new StaticData(logger);
        await staticData.LoadAsync(dir.Path);

        var student = staticData.StudentTable.Get(3);
        Assert.Equal(2, student.TeacherId);

        var teacher = staticData.TeacherTable.Get(student.TeacherId);
        Assert.Equal("이영희", teacher.Name);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_NonKeyFkViolation_ThrowsAggregateException()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Teacher.Sheet1.csv", ErrorTeacherCsv);
        dir.Write("Student.Sheet1.csv", StudentCsv);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyValidationTests>() is not TestOutputLogger<ForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("Error대학교", ex.InnerExceptions[0].Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public async Task Load_KeyFkViolation_ThrowsAggregateException()
    {
        using var dir = new CsvTestDirectory();
        dir.Write("School.Sheet1.csv", SchoolCsv);
        dir.Write("Teacher.Sheet1.csv", TeacherCsv);
        dir.Write("Student.Sheet1.csv", ErrorStudentCsv);

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ForeignKeyValidationTests>() is not TestOutputLogger<ForeignKeyValidationTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var staticData = new StaticData(logger);

        var ex = await Assert.ThrowsAsync<AggregateException>(() => staticData.LoadAsync(dir.Path));

        Assert.Single(ex.InnerExceptions);
        Assert.Contains("999", ex.InnerExceptions[0].Message);
        Assert.Empty(logger.Logs);
    }
}
