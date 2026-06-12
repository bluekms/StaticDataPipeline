using Microsoft.Extensions.Logging;
using Sdp.Attributes;
using Sdp.Csv;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.CsvMappingBehaviorTests;

public partial class NestedRecordMappingTests(ITestOutputHelper testOutputHelper)
{
    [StaticDataRecord("Nested", "Person")]
    public sealed partial record PersonRecord(int Id, string Name, PersonRecord.AddressData Address)
    {
        public sealed record AddressData(string City, string Street);
    }

    [Fact]
    public void MapNestedRecord()
    {
        var logger = CreateLogger();

        var csv = "Id,Name,Address.City,Address.Street\n1,Alice,Seoul,Gangnam";

        var record = Assert.Single(CsvLoader.Parse(csv, PersonRecord.MapFromCsvRow));

        Assert.Equal(1, record.Id);
        Assert.Equal("Alice", record.Name);
        Assert.Equal("Seoul", record.Address.City);
        Assert.Equal("Gangnam", record.Address.Street);
        logger.LogInformation("Person mapped: Name={Name}, Address={Address}", record.Name, record.Address);
    }

    public sealed record Contact(string Email, string Phone);

    public sealed record DetailedAddress(string City, string Street, string ZipCode);

    [StaticDataRecord("Nested", "Employee")]
    public sealed partial record EmployeeRecord(int Id, string Name, DetailedAddress Address, Contact Contact);

    [Fact]
    public void MapMultipleNestedRecords()
    {
        var logger = CreateLogger();

        var csv = "Id,Name,Address.City,Address.Street,Address.ZipCode,Contact.Email,Contact.Phone\n"
                + "42,Bob,Tokyo,Shibuya,150-0001,bob@example.com,123-456";

        var record = Assert.Single(CsvLoader.Parse(csv, EmployeeRecord.MapFromCsvRow));

        Assert.Equal(42, record.Id);
        Assert.Equal("Bob", record.Name);
        Assert.Equal("Tokyo", record.Address.City);
        Assert.Equal("Shibuya", record.Address.Street);
        Assert.Equal("150-0001", record.Address.ZipCode);
        Assert.Equal("bob@example.com", record.Contact.Email);
        Assert.Equal("123-456", record.Contact.Phone);
        logger.LogInformation("Employee mapped: Address={Address}, Contact={Contact}", record.Address, record.Contact);
    }

    [StaticDataRecord("Nested", "Outer")]
    public sealed partial record OuterRecord(int Id, [ColumnName("Middle")] OuterRecord.Middle MyMiddle)
    {
        public sealed record Middle(string Name, Middle.InnerMost Inner)
        {
            public sealed record InnerMost(int Value);
        }
    }

    [Fact]
    public void MapDeeplyNestedRecords()
    {
        var logger = CreateLogger();

        var csv = "Id,Middle.Name,Middle.Inner.Value\n1,Test,999";

        var record = Assert.Single(CsvLoader.Parse(csv, OuterRecord.MapFromCsvRow));

        Assert.Equal(1, record.Id);
        Assert.Equal("Test", record.MyMiddle.Name);
        Assert.Equal(999, record.MyMiddle.Inner.Value);
        logger.LogInformation(
            "Deeply nested record mapped: Middle.Name={Name}, Inner.Value={Value}",
            record.MyMiddle.Name,
            record.MyMiddle.Inner.Value);
    }

    public sealed record AddressWithColumnName(
        [ColumnName("도시")] string City,
        [ColumnName("거리")] string Street);

    [StaticDataRecord("Nested", "Korean")]
    public sealed partial record PersonWithColumnNameRecord(
        [ColumnName("번호")] int Id,
        [ColumnName("이름")] string Name,
        [ColumnName("주소")] AddressWithColumnName Address);

    [Fact]
    public void MapNestedRecordWithColumnNames()
    {
        var logger = CreateLogger();

        var csv = "번호,이름,주소.도시,주소.거리\n1,철수,서울,강남대로";

        var record = Assert.Single(CsvLoader.Parse(csv, PersonWithColumnNameRecord.MapFromCsvRow));

        Assert.Equal(1, record.Id);
        Assert.Equal("철수", record.Name);
        Assert.Equal("서울", record.Address.City);
        Assert.Equal("강남대로", record.Address.Street);
        logger.LogInformation(
            "Korean column record mapped: Name={Name}, Address={Address}",
            record.Name,
            record.Address);
    }

    private TestOutputLogger<NestedRecordMappingTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Information);
        if (factory.CreateLogger<NestedRecordMappingTests>() is not TestOutputLogger<NestedRecordMappingTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
