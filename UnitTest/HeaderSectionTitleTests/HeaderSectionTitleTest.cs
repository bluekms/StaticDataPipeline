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
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve("\t");

        Assert.Equal("### Headers (TSV)", title);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void CommaSeparator_ReturnsCsvLabel()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve(",");

        Assert.Equal("### Headers (CSV)", title);
        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData(";")]
    [InlineData("|")]
    [InlineData(" ")]
    [InlineData(" - ")]
    [InlineData("::")]
    public void OtherSeparator_ReturnsPlainHeadersLabel(string separator)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve(separator);

        Assert.Equal("### Headers", title);
        Assert.Empty(logger.Logs);
    }

    [Fact]
    public void EmptySeparator_ReturnsPlainHeadersLabel()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<HeaderSectionTitleTest>() is not TestOutputLogger<HeaderSectionTitleTest> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        var title = HeaderSectionTitle.Resolve(string.Empty);

        Assert.Equal("### Headers", title);
        Assert.Empty(logger.Logs);
    }
}
