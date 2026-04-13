using Microsoft.Extensions.Logging;
using SchemaInfoScanner;
using SchemaInfoScanner.Collectors;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.NotSupportedPropertyTypeSchemaTests.CollectionPropertySchemaTests;

public class NotSupportedCollectionTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("Queue")]
    [InlineData("Stack")]
    [InlineData("LinkedList")]
    [InlineData("ObservableCollection")]
    [InlineData("Collection")]
    [InlineData("ConcurrentBag")]
    [InlineData("ConcurrentQueue")]
    [InlineData("ConcurrentStack")]
    [InlineData("ImmutableList")]
    [InlineData("ImmutableHashSet")]
    [InlineData("ImmutableQueue")]
    [InlineData("ImmutableStack")]
    [InlineData("ImmutableSortedSet")]
    [InlineData("PriorityQueue")]
    [InlineData("BlockingCollection")]
    public void RejectsNestedCollectionTypes(string collection)
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<NotSupportedCollectionTests>() is not TestOutputLogger<NotSupportedCollectionTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        // language=C#
        var code = $"""
                     [StaticDataRecord("Test", "TestSheet")]
                     public sealed record MyRecord(
                         {collection}<int> Property,
                     );
                     """;

        var loadResult = RecordSchemaLoader.OnLoad(code, logger);
        Assert.Throws<NotSupportedException>(() => new RecordSchemaSet(loadResult, logger));
        Assert.Single(logger.Logs);
    }
}
