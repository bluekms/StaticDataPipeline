using ScenarioTest.Support;

namespace ScenarioTest.Tests.Declarations;

[Collection(CliCollectionDefinition.Name)]
public class DeclarationDefectTests
{
    [Theory]
    [InlineData("TwoKeys.cs", "must have at most one [Key] member")]
    [InlineData(
        "LengthWithSingleColumn.cs",
        "LengthAttribute and SingleColumnCollectionAttribute cannot be used together")]
    [InlineData("NullableWithoutNullString.cs", "nullable type must use NullStringAttribute")]
    [InlineData("ColumnNameForbiddenChar.cs", "ColumnName \"a,b\" contains a forbidden character")]
    [InlineData("MapValueWithoutKey.cs", "KeyAttribute must be used in one of the parameters")]
    [InlineData("CircularRecord.cs", "Circular reference detected")]
    [InlineData("UndeclaredType.cs", "is not found in the record source folder or in the runtime libraries")]
    [InlineData("BclEnumWithoutUsing.cs", "The type 'DayOfWeek'", "the project's implicit usings are not applied")]
    [InlineData("FkAndSwitchFkTogether.cs", "cannot be used together on the same parameter")]
    [InlineData("GuidWithRange.cs", "RangeAttribute cannot be used")]
    [InlineData("GuidWithDateTimeFormat.cs", "DateTimeFormatAttribute cannot be used")]
    [InlineData("DateOnlyWithoutFormat.cs", "must use DateTimeFormatAttribute")]
    public void DefectiveRecordDeclaration_FailsBeforeReadingExcel(string recordFile, params string[] expected)
    {
        using var workspace = new ScenarioWorkspace();

        var result = workspace.RunExtractor(
            "-r",
            workspace.InvalidRecord(recordFile),
            "-e",
            workspace.ExcelSchool,
            "-o",
            workspace.CsvSchool,
            "-s",
            "B3");

        ExtractionAssert.FailsWith(result, expected);
        ExtractionAssert.HasStackTrace(result);
        Assert.False(Directory.Exists(workspace.CsvSchool));
    }

    [Fact]
    public void SameTypeDeclaredInTwoFiles_FailsWithCompileError()
    {
        using var workspace = new ScenarioWorkspace();
        var schoolTypes = Path.Combine(workspace.RecordsSchool, "SchoolTypes.cs");
        File.Copy(schoolTypes, Path.Combine(workspace.RecordsSchool, "SchoolTypesCopy.cs"));

        var result = workspace.RunSchool();

        ExtractionAssert.FailsWith(result, "CS0101", "already contains a definition for 'Weekday'");
        Assert.False(Directory.Exists(workspace.CsvSchool));
    }
}
