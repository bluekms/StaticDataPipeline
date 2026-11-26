using System.Collections.Frozen;
using System.Collections.Immutable;
using Sdp.Attributes;

namespace ScenarioTest.Records.School;

// 급식
[StaticDataRecord("School", "Cafeteria")]
public sealed partial record CafeteriaRecord(
    [Key] MealSlot Slot,

    [SingleColumnCollection(",")]
    [CountRange(1, 8)]
    ImmutableArray<string> Menu,

    // Allergens 는 식단표에 적는 알레르기 유발 식품 번호(1 난류 ~ 19 잣)
    // "5.6" 처럼 숫자로 읽힐 수 있어 샘플 엑셀에 문자열 셀로 넣는다.
    [SingleColumnCollection(".")] FrozenSet<byte> Allergens,

    ushort Calories);
