using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [Length] 는 열을 펼치고 [SingleColumnCollection] 은 한 셀에 담으므로 함께 쓸 수 없다.
[StaticDataRecord("Invalid", "Cafeteria")]
public sealed partial record CafeteriaRecord(
    [Key] int Id,
    [Length(4)] [SingleColumnCollection(",")] ImmutableArray<string> Menu);
