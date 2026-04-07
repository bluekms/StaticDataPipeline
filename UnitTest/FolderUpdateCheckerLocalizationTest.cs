using System.Globalization;
using ExcelColumnExtractor.Checkers;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest;

public class FolderUpdateCheckerLocalizationTest(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("en", "File added: Foo")]
    [InlineData("ko", "파일이 추가되었습니다: Foo")]
    public void AddedMessage_ByLocale(string locale, string expected)
    {
        var savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try
        {
            var now = DateTime.UtcNow;
            var dummy = new Dictionary<string, DateTime> { { "dummy", now } };
            var added = new Dictionary<string, DateTime>
            {
                { "dummy", now },
                { "Foo", now }
            };

            var before = new FolderState("DummyPath", dummy);
            var after = new FolderState("DummyPath", added);

            var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
            if (factory.CreateLogger<FolderUpdateCheckerLocalizationTest>() is not TestOutputLogger<FolderUpdateCheckerLocalizationTest> logger)
            {
                throw new InvalidOperationException("Logger creation failed.");
            }

            FolderUpdateChecker.Check(before, after, logger);

            Assert.Single(logger.Logs);
            Assert.Equal(expected, logger.Logs.First().Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }
    }

    [Theory]
    [InlineData("en", "File removed: Foo")]
    [InlineData("ko", "파일이 삭제되었습니다: Foo")]
    public void RemovedMessage_ByLocale(string locale, string expected)
    {
        var savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try
        {
            var now = DateTime.UtcNow;
            var dummy = new Dictionary<string, DateTime>
            {
                { "dummy", now },
                { "Foo", now }
            };
            var removed = new Dictionary<string, DateTime> { { "dummy", now } };

            var before = new FolderState("DummyPath", dummy);
            var after = new FolderState("DummyPath", removed);

            var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
            if (factory.CreateLogger<FolderUpdateCheckerLocalizationTest>() is not TestOutputLogger<FolderUpdateCheckerLocalizationTest> logger)
            {
                throw new InvalidOperationException("Logger creation failed.");
            }

            FolderUpdateChecker.Check(before, after, logger);

            Assert.Single(logger.Logs);
            Assert.Equal(expected, logger.Logs.First().Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }
    }

    [Theory]
    [InlineData("en", "File changed since last scan: dummy")]
    [InlineData("ko", "마지막 스캔 이후 변경된 파일: dummy")]
    public void UpdatedMessage_ByLocale(string locale, string expected)
    {
        var savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try
        {
            var now = DateTime.UtcNow;
            var beforeState = new Dictionary<string, DateTime> { { "dummy", now.AddSeconds(-1) } };
            var afterState = new Dictionary<string, DateTime> { { "dummy", now } };

            var before = new FolderState("DummyPath", beforeState);
            var after = new FolderState("DummyPath", afterState);

            var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Trace);
            if (factory.CreateLogger<FolderUpdateCheckerLocalizationTest>() is not TestOutputLogger<FolderUpdateCheckerLocalizationTest> logger)
            {
                throw new InvalidOperationException("Logger creation failed.");
            }

            FolderUpdateChecker.Check(before, after, logger);

            Assert.Single(logger.Logs);
            Assert.Equal(expected, logger.Logs.First().Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }
    }
}
