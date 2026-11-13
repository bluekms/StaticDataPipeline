using ClosedXML.Excel;
using ScenarioTest.Support;

namespace ScenarioTest.Tests.Normal;

[Collection(CliCollectionDefinition.Name)]
public class HeaderGeneratorTests(ExtractedCsvFixture fixture)
{
    [Fact]
    public void AllHeaders_MatchGolden_AndSkipIgnoredDraftRecord()
    {
        ExtractionAssert.Succeeded(fixture.AllHeadersResult);
        Golden.AssertEquals("AllHeaders.md", fixture.AllHeadersPath);
        var text = File.ReadAllText(fixture.AllHeadersPath);
        Assert.DoesNotContain("DraftBuffRecord", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SortCache", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedHeaders_AreAllPresentInWorkbookSheets()
    {
        ExtractionAssert.Succeeded(fixture.AllHeadersResult);

        var sections = HeaderSection.Parse(File.ReadAllLines(fixture.AllHeadersPath));

        Assert.Equal(27, sections.Count);
        foreach (var section in sections)
        {
            var sheetHeaders = ReadSheetHeaders(section);
            foreach (var header in section.Headers)
            {
                var present = sheetHeaders.Contains(header) || sheetHeaders.Contains(header + ".Value");
                Assert.True(present, FormattableString.Invariant(
                    $"{section.Workbook}.{section.Sheet} 에 {section.RecordName} 의 헤더 '{header}' 가 없다."));
            }
        }

        var students = sections.Single(x => x.RecordName.EndsWith(".StudentRecord", StringComparison.Ordinal));
        Assert.DoesNotContain("비고", students.Headers);
        Assert.Contains("비고", ReadSheetHeaders(students));
        var grades = sections.Single(x => x.RecordName.EndsWith(".GradeRecord", StringComparison.Ordinal));
        Assert.Equal("StudentId", grades.Headers[0]);
    }

    [Fact]
    public void SingleRecordHeader_WithCommaSeparator_UsesCsvLabel()
    {
        using var workspace = new ScenarioWorkspace();
        var tabOutput = Path.Combine(workspace.Root, "headers", "Student.md");
        var commaOutput = Path.Combine(workspace.Root, "headers", "StudentComma.md");

        var tab = workspace.RunHeaderGenerator(
            "header",
            "-r",
            workspace.Records,
            "-n",
            "StudentRecord",
            "-o",
            tabOutput);
        var comma = workspace.RunHeaderGenerator(
            "header",
            "-r",
            workspace.Records,
            "-n",
            "StudentRecord",
            "--separator",
            ",",
            "-o",
            commaOutput);

        ExtractionAssert.Succeeded(tab);
        ExtractionAssert.Succeeded(comma);
        Assert.Contains("### Headers (TSV)", File.ReadAllText(tabOutput), StringComparison.Ordinal);
        var commaText = File.ReadAllText(commaOutput);
        Assert.Contains("### Headers (CSV)", commaText, StringComparison.Ordinal);
        Assert.Contains("Id,이름,Gender,BirthDate", commaText, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedRecordName_IsNotAStaticDataRecord_AndFailsWithExitCode1()
    {
        using var workspace = new ScenarioWorkspace();

        var result = workspace.RunHeaderGenerator("header", "-r", workspace.Records, "-n", "SubjectScore");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(result.Errors, x => x.Contains("SubjectScore", StringComparison.Ordinal));
    }

    private HashSet<string> ReadSheetHeaders(HeaderSection section)
    {
        const int headerRow = 3;
        using var book = new XLWorkbook(fixture.WorkbookPath(section.Workbook));
        var sheet = book.Worksheet(section.Sheet);

        return sheet.Row(headerRow)
            .CellsUsed()
            .Select(x => x.GetString())
            .ToHashSet(StringComparer.Ordinal);
    }

    private sealed record HeaderSection(string RecordName, string Workbook, string Sheet, List<string> Headers)
    {
        public static List<HeaderSection> Parse(string[] lines)
        {
            var sections = new List<HeaderSection>();
            for (var i = 0; i < lines.Length; i++)
            {
                var isSectionTitle = lines[i].StartsWith("## ", StringComparison.Ordinal);
                if (!isSectionTitle)
                {
                    continue;
                }

                var recordName = lines[i][3..];
                var workbook = Between(lines[i + 1], "`", ".xlsx`");
                var sheet = Between(lines[i + 2], "`", "`");
                var headers = new List<string>();
                var cursor = i + 5;
                while (cursor < lines.Length && lines[cursor].StartsWith("- ", StringComparison.Ordinal))
                {
                    headers.Add(lines[cursor][2..]);
                    cursor++;
                }

                sections.Add(new HeaderSection(recordName, workbook, sheet, headers));
            }

            return sections;
        }

        private static string Between(string line, string open, string close)
        {
            var start = line.IndexOf(open, StringComparison.Ordinal) + open.Length;
            var end = line.IndexOf(close, start, StringComparison.Ordinal);

            return line[start..end];
        }
    }
}
