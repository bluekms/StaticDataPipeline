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

// 주기율표. 시트 하나뿐인 워크북을 시작 셀 옵션 없이(A1) 따로 추출하는 경로의 입력이다.
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
