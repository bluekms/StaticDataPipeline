using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 급식. Allergens 는 "5.6" 처럼 숫자로 읽힐 수 있어 픽스처에 문자열 셀로 넣는다.
[StaticDataRecord("School", "Cafeteria")]
public sealed partial record CafeteriaRecord(
    [Key] MealSlot Slot,
    [SingleColumnCollection(",")] [CountRange(1, 8)] ImmutableArray<string> Menu,
    [SingleColumnCollection(".")] FrozenSet<byte> Allergens,
    ushort Calories);
