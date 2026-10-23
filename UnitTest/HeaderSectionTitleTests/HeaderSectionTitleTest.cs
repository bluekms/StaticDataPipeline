using Microsoft.Extensions.Logging;
using StaticDataHeaderGenerator;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.HeaderSectionTitleTests;

public partial class HeaderSectionTitleTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TabSeparator_ReturnsTsvLabel()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve("\t");
        logger.LogInformation("Resolved title: {Title}", title);

        Assert.Equal("### Headers (TSV)", title);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void CommaSeparator_ReturnsCsvLabel()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve(",");
        logger.LogInformation("Resolved title: {Title}", title);

        Assert.Equal("### Headers (CSV)", title);
        Assert.Single(logger.Logs);
    }

    [Theory]
    [InlineData(";")]
    [InlineData("|")]
    [InlineData(" ")]
    [InlineData(" - ")]
    [InlineData("::")]
    public void OtherSeparator_ReturnsPlainHeadersLabel(string separator)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve(separator);
        logger.LogInformation("Separator: {Separator}, Title: {Title}", separator, title);

        Assert.Equal("### Headers", title);
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void EmptySeparator_ReturnsPlainHeadersLabel()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve(string.Empty);
        logger.LogInformation("Resolved title: {Title}", title);

        Assert.Equal("### Headers", title);
        Assert.Single(logger.Logs);
    }
}
