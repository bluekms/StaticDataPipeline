using Eds.Csv;

namespace UnitTest.CsvRecordMapperTests;

public class SingleParameterRecordMapperTests
{
    // 싱글 파라메터 래핑 레코드
    public sealed record Identifier(int Value);

    public sealed record Entity(Identifier Id, string Name);

    [Fact]
    public void MapSingleParameterRecord_UsesRecordNameAsHeader()
    {
        // Id.Value 대신 Id만 사용
        var headers = new[] { "Id", "Name" };
        var values = new[] { "42", "Test Entity" };

        var result = CsvRecordMapper.MapToRecord<Entity>(headers, values);

        Assert.Equal(42, result.Id.Value);
        Assert.Equal("Test Entity", result.Name);
    }

    public sealed record UserId(long Value);

    public sealed record ProductId(int Value);

    public sealed record Order(UserId UserId, ProductId ProductId, int Quantity);

    [Fact]
    public void MapMultipleSingleParameterRecords()
    {
        var headers = new[] { "UserId", "ProductId", "Quantity" };
        var values = new[] { "1001", "500", "3" };

        var result = CsvRecordMapper.MapToRecord<Order>(headers, values);

        Assert.Equal(1001L, result.UserId.Value);
        Assert.Equal(500, result.ProductId.Value);
        Assert.Equal(3, result.Quantity);
    }

    public sealed record DecimalId(decimal Value);

    public sealed record Price(DecimalId Id, decimal Amount);

    [Fact]
    public void MapSingleParameterRecord_WithDecimalType()
    {
        var headers = new[] { "Id", "Amount" };
        var values = new[] { "123.456", "999.99" };

        var result = CsvRecordMapper.MapToRecord<Price>(headers, values);

        Assert.Equal(123.456m, result.Id.Value);
        Assert.Equal(999.99m, result.Amount);
    }

    // 멀티 파라메터 레코드는 기존 방식 유지
    public sealed record MultiParamRecord(int X, int Y);

    public sealed record ContainerWithMultiParam(MultiParamRecord Point, string Label);

    [Fact]
    public void MapMultiParameterRecord_StillUsesNestedHeaders()
    {
        // 멀티 파라메터 레코드는 기존처럼 Point.X, Point.Y 형태 사용
        var headers = new[] { "Point.X", "Point.Y", "Label" };
        var values = new[] { "10", "20", "Origin" };

        var result = CsvRecordMapper.MapToRecord<ContainerWithMultiParam>(headers, values);

        Assert.Equal(10, result.Point.X);
        Assert.Equal(20, result.Point.Y);
        Assert.Equal("Origin", result.Label);
    }

    [Fact]
    public void MapSingleParameterRecord_BackwardCompatibility_WithValueSuffix()
    {
        // 기존 Id.Value 형태도 정상 동작해야 함 (하위 호환)
        var headers = new[] { "Id.Value", "Name" };
        var values = new[] { "99", "Legacy Entity" };

        var result = CsvRecordMapper.MapToRecord<Entity>(headers, values);

        Assert.Equal(99, result.Id.Value);
        Assert.Equal("Legacy Entity", result.Name);
    }
}
