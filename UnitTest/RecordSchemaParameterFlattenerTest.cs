using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest;

public class RecordSchemaParameterFlattenerTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void MyClassTest()
    {
        var code = @"
            public sealed record Subject(
                string Name,
                [Length(2)] ImmutableArray<int> QuarterScore
            );

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record MyClass(
                string Name,
                [Length(3)] ImmutableArray<Subject> SubjectA,
                int Age,
                [Length(4)] ImmutableArray<Subject> SubjectB
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        Assert.Equal(23, results.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MyClassWithSingleColumnTest()
    {
        var code = @"
            public sealed record Subject(
                string Name,
                [SingleColumnCollection("", "")][Length(4)] ImmutableArray<int> QuarterScore
            );

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record MyClass(
                string Name,
                [Length(3)] ImmutableArray<Subject> SubjectA,
                int Age,
                [Length(4)] ImmutableArray<Subject> SubjectB
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        Assert.Equal(16, results.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MyClassWithSingleColumnAndColumnNameTest()
    {
        var code = @"
            public sealed record Subject(
                string Name,
                [SingleColumnCollection("", "")][ColumnName(""QuarterScores"")][Length(4)] ImmutableArray<int> QuarterScore
            );

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record MyClass(
                string Name,
                [Length(3)] ImmutableArray<Subject> SubjectA,
                int Age,
                [Length(4)] ImmutableArray<Subject> SubjectB
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        Assert.Equal(16, results.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MyClassWithNameAttributesTest()
    {
        var code = @"
            public sealed record Subject(
                [ColumnName(""Bar"")] string Name,
                [ColumnName(""Scores"")][Length(2)] ImmutableArray<int> QuarterScore
            );

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record MyClass(
                string Name,
                [Length(3)][ColumnName(""SubjectF"")] ImmutableArray<Subject> SubjectA,
                int Age,
                [Length(4)]ImmutableArray<Subject> SubjectB,
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        Assert.Equal(23, results.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DictionaryWithPrimitiveKeyTest()
    {
        var code = @"
            public sealed record MyRecord([Key] int Id, int Value);

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record MyStaticData(
                string Name,
                [Length(3)] FrozenDictionary<int, MyRecord> MyDictionary,
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        Assert.Equal(7, results.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void DictionaryWithRecordKeyTest()
    {
        var code = @"
            public sealed record Human(string Name, int Age);
            public sealed record MyRecord([Key] Human Human, int Value);

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record MyStaticData(
                [Length(3)] FrozenDictionary<Human, MyRecord> MyDictionary,
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        Assert.Equal(9, results.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void CompanyTest()
    {
        var code = @"
            public sealed record Address(string Street, string City);
            public sealed record ContactInfo(string PhoneNumber, string Email);
            public sealed record Project(string ProjectName, [Length(6)] ImmutableArray<string> TeamMembers, double Budget);
            public sealed record Department(string DepartmentName, [Length(2)] ImmutableArray<Project> Projects);

            public sealed record Employee(
                string FullName,
                int Age,
                Address HomeAddress,
                ContactInfo Contact,
                string Position,
                [Length(3)] ImmutableArray<Department> Departments
            );

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record Company(
                string CompanyName,
                Address HeadquartersAddress,
                [Length(5)] FrozenSet<Employee> Employees,
                [Length(3)] ImmutableArray<Department> CoreDepartments
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        Assert.Equal(344, results.Count);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void SingleParameterRecordTest()
    {
        var code = @"
            public sealed record SchoolId(int Value);
            public sealed record TeacherId(int Value);

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record School(
                SchoolId Id,
                string Name,
                TeacherId MainTeacher
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        // 싱글 파라메터 레코드는 Id.Value가 아닌 Id로 출력
        Assert.Equal(3, results.Count);
        Assert.Equal("Id", results[0]);
        Assert.Equal("Name", results[1]);
        Assert.Equal("MainTeacher", results[2]);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void MixedSingleAndMultiParameterRecordTest()
    {
        var code = @"
            public sealed record EntityId(int Value);
            public sealed record Address(string City, string Street);

            [StaticDataRecord(""Test"", ""TestSheet"")]
            public sealed record Entity(
                EntityId Id,
                string Name,
                Address Location
            );";

        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<RecordSchemaParameterFlattenerTest>() is not TestOutputLogger<RecordSchemaParameterFlattenerTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var parseResult = SimpleCordParser.Parse(code, logger);

        var results = RecordFlattener.Flatten(
            parseResult.RawRecordSchemata[0],
            parseResult.RecordSchemaCatalog,
            logger);

        foreach (var header in results)
        {
            testOutputHelper.WriteLine(header);
        }

        // EntityId는 싱글 파라메터 → Id
        // Address는 멀티 파라메터 → Location.City, Location.Street
        Assert.Equal(4, results.Count);
        Assert.Equal("Id", results[0]);
        Assert.Equal("Name", results[1]);
        Assert.Equal("Location.City", results[2]);
        Assert.Equal("Location.Street", results[3]);
        Assert.Empty(logger.Logs);
    }
}
