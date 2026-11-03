using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.Invalid;

// [Length] 와 [SingleColumnCollection] 을 함께 붙임. 열을 펼치는 방식이 충돌하므로 스캐너가 거부해야 한다.
[StaticDataRecord("Invalid", "Cafeteria")]
public sealed partial record CafeteriaRecord(
    [Key] int Id,
    [Length(4)] [SingleColumnCollection(",")] ImmutableArray<string> Menu);
