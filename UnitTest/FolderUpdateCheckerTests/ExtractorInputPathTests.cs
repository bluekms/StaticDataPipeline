using System.Globalization;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;
using ExtractorMessages = ExcelColumnExtractor.Resources.Messages;

namespace UnitTest.FolderUpdateCheckerTests;

public class ExtractorInputPathTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void Main_MissingRecordPath_ReportsOptionAndPath()
    {
        var logger = CreateLogger();
        using var directory = new CsvTestDirectory();
        var missing = Path.Combine(directory.Path, "Records");

        var ex = Assert.Throws<ArgumentException>(() => ExcelColumnExtractor.Program.Main(["-r", missing, "-e", directory.Path, "-o", directory.Path]));

        var expected = string.Format(CultureInfo.CurrentCulture, ExtractorMessages.Composite.OptionPathNotExist, "record-path", Path.GetFullPath(missing));
        Assert.Equal(expected, ex.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void Main_MissingExcelPath_ReportsOptionAndPath()
    {
        var logger = CreateLogger();
        using var directory = new CsvTestDirectory();
        var missing = Path.Combine(directory.Path, "Excelz");

        var ex = Assert.Throws<ArgumentException>(() => ExcelColumnExtractor.Program.Main(["-r", directory.Path, "-e", missing, "-o", directory.Path]));

        var expected = string.Format(CultureInfo.CurrentCulture, ExtractorMessages.Composite.OptionPathNotExist, "excel-path", Path.GetFullPath(missing));
        Assert.Equal(expected, ex.Message);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void FolderStateScanner_FilePath_TracksThatFileOnly()
    {
        var logger = CreateLogger();
        using var directory = new CsvTestDirectory();
        directory.Write("A.cs", "// a");
        directory.Write("B.cs", "// b");

        var state = FolderStateScanner.Scan(Path.Combine(directory.Path, "A.cs"), ".cs");

        var fileName = Assert.Single(state.FileStates.Keys);
        Assert.Equal("A.cs", fileName);
        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<ExtractorInputPathTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ExtractorInputPathTests>() is not TestOutputLogger<ExtractorInputPathTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
