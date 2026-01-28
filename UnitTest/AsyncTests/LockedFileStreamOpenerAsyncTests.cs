using ExcelColumnExtractor.Scanners;

namespace UnitTest.AsyncTests;

[Collection("ExcelFileTests")]
public class LockedFileStreamOpenerAsyncTests
{
    private static string GetTestExcelPath()
    {
        return Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!,
            "..",
            "..",
            "..",
            "..",
            "Docs",
            "SampleExcel",
            "Excel1.xlsx");
    }

    [Fact]
    public async Task CreateAsync_WithValidFile_ReturnsOpener()
    {
        var excelPath = GetTestExcelPath();
        Assert.True(File.Exists(excelPath), $"Test file not found: {excelPath}");

        using var opener = await LockedFileStreamOpener.CreateAsync(excelPath);

        Assert.NotNull(opener);
        Assert.NotNull(opener.Stream);
        Assert.True(opener.Stream.CanRead);
    }

    [Fact]
    public async Task CreateAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        var excelPath = GetTestExcelPath();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            LockedFileStreamOpener.CreateAsync(excelPath, cts.Token));
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}.xlsx");

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            LockedFileStreamOpener.CreateAsync(invalidPath));
    }

    [Fact]
    public async Task CreateAsync_StreamDisposesCorrectly()
    {
        var excelPath = GetTestExcelPath();

        var opener = await LockedFileStreamOpener.CreateAsync(excelPath);
        var stream = opener.Stream;
        opener.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
    }

    [Fact]
    public async Task CreateAsync_IsTemp_IsFalseForUnlockedFile()
    {
        var excelPath = GetTestExcelPath();
        using var opener = await LockedFileStreamOpener.CreateAsync(excelPath);

        Assert.False(opener.IsTemp);
    }
}
