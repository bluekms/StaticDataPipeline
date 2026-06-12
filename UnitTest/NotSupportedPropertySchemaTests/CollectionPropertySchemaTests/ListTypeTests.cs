using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.NotSupportedPropertySchemaTests.CollectionPropertySchemaTests;

public partial class ListTypeTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("List")]
    [InlineData("HashSet")]
    public void RejectsNestedCollectionTypes(string collection)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ListTypeTests>() is not TestOutputLogger<ListTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                    [StaticDataRecord("Test", "TestSheet")]
                    public sealed partial record MyRecord(
                        ImmutableArray<{collection}<int>> Property,
                    );
                    """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }

    [Fact]
    public void RejectsNestedDictionaryTypes()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ListTypeTests>() is not TestOutputLogger<ListTypeTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = """
                   [StaticDataRecord("Test", "TestSheet")]
                   public sealed partial record MyRecord(
                       List<Dictionary<int, string>> Property,
                   );
                   """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }
}
