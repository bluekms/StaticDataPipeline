using Sdp.Attributes;

namespace ScenarioTest.Records.School;

public record struct BookId(int Value);

// 도서. 다른 Record 와 달리 BookId 브랜딩을 Record 밖에 선언했다. Library 워크북의 대출과 예약이 가리키는 대상이다.
[StaticDataRecord("Library", "Books")]
public sealed partial record BookRecord(
    [Key] BookId Id,
    [NullString("")] [Range(typeof(ulong), "9780000000000", "9799999999999")] ulong? Isbn13,
    string Title,
    [Range(typeof(string), "000", "999.99")] string CallNumber,
    short PublishedYear,
    [NullString("-")] char? ShelfCode);
