using Sdp.Attributes;
using Sdp.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class NestedRecordMapperTests
{
    public sealed record Person(int Id, string Name, Person.AddressData Address)
    {
        public sealed record AddressData(string City, string Street);
    }

    [Fact]
    public void MapNestedRecord()
    {
        var headers = new[] { "Id", "Name", "Address.City", "Address.Street" };
        var values = new[] { "1", "Alice", "Seoul", "Gangnam" };

        var result = CsvRecordMapper.MapToRecord<Person>(headers, values);

        Assert.Equal(1, result.Id);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("Seoul", result.Address.City);
        Assert.Equal("Gangnam", result.Address.Street);
    }

    public sealed record Contact(string Email, string Phone);

    public sealed record DetailedAddress(string City, string Street, string ZipCode);

    public sealed record Employee(int Id, string Name, DetailedAddress Address, Contact Contact);

    [Fact]
    public void MapMultipleNestedRecords()
    {
        var headers = new[]
        {
            "Id", "Name",
            "Address.City", "Address.Street", "Address.ZipCode",
            "Contact.Email", "Contact.Phone",
        };
        var values = new[]
        {
            "42", "Bob",
            "Tokyo", "Shibuya", "150-0001",
            "bob@example.com", "123-456",
        };

        var result = CsvRecordMapper.MapToRecord<Employee>(headers, values);

        Assert.Equal(42, result.Id);
        Assert.Equal("Bob", result.Name);
        Assert.Equal("Tokyo", result.Address.City);
        Assert.Equal("Shibuya", result.Address.Street);
        Assert.Equal("150-0001", result.Address.ZipCode);
        Assert.Equal("bob@example.com", result.Contact.Email);
        Assert.Equal("123-456", result.Contact.Phone);
    }

    public sealed record Outer(int Id, [ColumnName("Middle")] Outer.Middle MyMiddle)
    {
        public sealed record Middle(string Name, Middle.InnerMost Inner)
        {
            public sealed record InnerMost(int Value);
        }
    }

    [Fact]
    public void MapDeeplyNestedRecords()
    {
        var headers = new[] { "Id", "Middle.Name", "Middle.Inner.Value" };
        var values = new[] { "1", "Test", "999" };

        var result = CsvRecordMapper.MapToRecord<Outer>(headers, values);

        Assert.Equal(1, result.Id);
        Assert.Equal("Test", result.MyMiddle.Name);
        Assert.Equal(999, result.MyMiddle.Inner.Value);
    }

    public sealed record AddressWithColumnName(
        [ColumnName("도시")] string City,
        [ColumnName("거리")] string Street);

    public sealed record PersonWithColumnName(
        [ColumnName("번호")] int Id,
        [ColumnName("이름")] string Name,
        [ColumnName("주소")] AddressWithColumnName Address);

    [Fact]
    public void MapNestedRecordWithColumnNames()
    {
        var headers = new[] { "번호", "이름", "주소.도시", "주소.거리" };
        var values = new[] { "1", "철수", "서울", "강남대로" };

        var result = CsvRecordMapper.MapToRecord<PersonWithColumnName>(headers, values);

        Assert.Equal(1, result.Id);
        Assert.Equal("철수", result.Name);
        Assert.Equal("서울", result.Address.City);
        Assert.Equal("강남대로", result.Address.Street);
    }
}
