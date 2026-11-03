using Sdp.Attributes;

namespace ScenarioTest.Records.Science;

public enum ElementCategory
{
    Nonmetal,
    NobleGas,
    AlkaliMetal,
    AlkalineEarthMetal,
    TransitionMetal,
    Lanthanide,
    PostTransitionMetal,
    Metalloid,
    Halogen,
}

// 주기율표. 시트 하나뿐인 워크북을 시작 셀 옵션 없이(A1) 따로 추출하는 실행 경로를 본다.
// byte 키 Range, RegularExpression(원소 기호), double/float/decimal/sbyte/short 와 그 nullable 을
// 서로 다른 NullString("N/A", "", "고대")으로 읽는다.
[StaticDataRecord("Science", "Elements")]
public sealed partial record ElementRecord(
    [Key] [Range(1, 118)] byte Number,
    [RegularExpression(@"^[A-Z][a-z]{0,2}$")] string Symbol,
    string Name,
    double AtomicMass,
    [NullString("N/A")] double? Density,
    [NullString("N/A")] float? Electronegativity,
    [NullString("N/A")] decimal? MeltingPointK,
    [NullString("")] [Range(1, 18)] byte? Group,
    [NullString("")] sbyte? CommonOxidation,
    [NullString("고대")] short? DiscoveredYear,
    ElementCategory Category);
