using Microsoft.CodeAnalysis;

namespace Sdp.SourceGenerator;

internal static class SdpDiagnostics
{
    public const string Category = "Sdp.SourceGenerator";

    public static readonly DiagnosticDescriptor RecordMustBePartial = new(
        id: "SDP0001",
        title: "StaticDataRecord must be partial",
        messageFormat: "[StaticDataRecord]를 부착한 '{0}'은 partial로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
        id: "SDP0002",
        title: "Containing type must be partial",
        messageFormat: "Sdp 소스 생성 대상을 감싸는 타입 '{0}'은 partial로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleKeyAttributes = new(
        id: "SDP0003",
        title: "Only one [Key] attribute is allowed per record",
        messageFormat: "record '{0}'에는 [Key]를 한 파라미터에만 부착할 수 있습니다 (현재 {1}개)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedMapperParameter = new(
        id: "SDP0004",
        title: "CSV mapper does not support this parameter",
        messageFormat: "record '{0}'의 파라미터 [{1}]은(는) CSV 매퍼가 지원하지 않는 타입/구성입니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CountRangeAndLengthMutuallyExclusive = new(
        id: "SDP0005",
        title: "CountRange and Length are mutually exclusive",
        messageFormat: "record '{0}'의 파라미터 '{1}'은 [CountRange]와 [Length]를 함께 사용할 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RecordMustHaveSinglePrimaryConstructor = new(
        id: "SDP0006",
        title: "Record must declare a primary constructor",
        messageFormat: "record '{0}'은 primary 생성자가 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CountRangeRequiresSingleColumnCollection = new(
        id: "SDP0007",
        title: "CountRange requires SingleColumnCollection",
        messageFormat: "record '{0}'의 파라미터 '{1}'의 [CountRange]는 [SingleColumnCollection]과 함께만 사용할 수 있습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NullableCollectionNotSupported = new(
        id: "SDP0008",
        title: "Collection parameter must not be nullable",
        messageFormat: "record '{0}'의 파라미터 '{1}'은 nullable 컬렉션이라 지원하지 않습니다 (컬렉션 자체는 non-nullable, 원소만 nullable 가능 - 예: ImmutableArray<int?>는 가능, ImmutableArray<int>?는 불가)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SingleColumnCollectionNotAllowedOnDictionary = new(
        id: "SDP0009",
        title: "SingleColumnCollection cannot be applied to FrozenDictionary",
        messageFormat: "record '{0}'의 파라미터 '{1}'은 FrozenDictionary이므로 [SingleColumnCollection]을 사용할 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RangeBoundOutOfTypeRange = new(
        id: "SDP0010",
        title: "Range bound is out of the parameter type's range",
        messageFormat: "record '{0}'의 파라미터 '{1}'의 [Range] 경계값이 파라미터 타입의 표현 가능 범위를 벗어났습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RedundantTypedRange = new(
        id: "SDP0011",
        title: "Use the numeric [Range] form instead of the typed form",
        messageFormat: "record '{0}'의 파라미터 '{1}'의 [Range]는 typeof 인자 형태 대신 숫자 형태 [Range({2}, {3})]로 작성하세요",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RecordCannotBeGeneric = new(
        id: "SDP0012",
        title: "StaticDataRecord cannot be generic",
        messageFormat: "[StaticDataRecord]를 부착한 '{0}'은 제네릭 타입이거나 제네릭 타입 안에 중첩될 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LengthAndSingleColumnCollectionMutuallyExclusive = new(
        id: "SDP0013",
        title: "Length and SingleColumnCollection are mutually exclusive",
        messageFormat: "record '{0}'의 파라미터 '{1}'은 [Length]와 [SingleColumnCollection]을 함께 사용할 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor LengthMustBePositive = new(
        id: "SDP0014",
        title: "Length must be at least 1",
        messageFormat: "record '{0}'의 파라미터 '{1}'의 [Length] 값은 1 이상이어야 합니다 (현재 {2})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RangeMinimumExceedsMaximum = new(
        id: "SDP0015",
        title: "Range minimum must not exceed maximum",
        messageFormat: "record '{0}'의 파라미터 '{1}'의 범위 최솟값({2})이 최댓값({3})보다 큽니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AttributeNotApplicable = new(
        id: "SDP0016",
        title: "Attribute is not applicable to this parameter type",
        messageFormat: "record '{0}'의 파라미터 '{1}'의 타입에는 [{2}]를 적용할 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FrozenDictionaryValueMustHaveSingleKey = new(
        id: "SDP0017",
        title: "FrozenDictionary value record must have exactly one [Key] parameter",
        messageFormat: "record '{0}'의 파라미터 '{1}': FrozenDictionary value record '{2}'에는 [Key] 파라미터가 정확히 1개 선언되어야 합니다 (현재 {3}개)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FrozenDictionaryKeyTypeMismatch = new(
        id: "SDP0018",
        title: "FrozenDictionary key type must match the value record's [Key] parameter",
        messageFormat: "record '{0}'의 파라미터 '{1}': FrozenDictionary 키 타입 '{2}'은(는) value record '{3}'의 non-nullable [Key] 파라미터 '{4}' (type '{5}')와(과) 일치해야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CountRangeMustBeNonNegative = new(
        id: "SDP0019",
        title: "CountRange bounds must be non-negative",
        messageFormat: "record '{0}'의 파라미터 '{1}'의 [CountRange] 경계({2}, {3})는 음수일 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TableSetRecordMustBePartial = new(
        id: "SDP0202",
        title: "TableSet record must be partial",
        messageFormat: "TableSet record '{0}'은 partial로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticDataTableMustBePartial = new(
        id: "SDP0203",
        title: "StaticDataTable subclass must be partial",
        messageFormat: "StaticDataTable을 상속한 '{0}'은 partial로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ForeignKeySwitchConflict = new(
        id: "SDP0204",
        title: "[ForeignKey] and [SwitchForeignKey] cannot coexist",
        messageFormat: "record '{0}'의 파라미터 '{1}'에 [ForeignKey]와 [SwitchForeignKey]가 함께 부착될 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ForeignKeyTargetNotFound = new(
        id: "SDP0205",
        title: "Foreign key target TableSet member not found",
        messageFormat: "FK 타겟 TableSet 멤버 '{0}'을 TableSet에서 찾을 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ForeignKeyTargetColumnNotFound = new(
        id: "SDP0206",
        title: "Foreign key target column not found",
        messageFormat: "FK 타겟 컬럼 '{0}'을 record '{1}'에서 찾을 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ForeignKeyTargetIsSingleColumnCollection = new(
        id: "SDP0207",
        title: "Foreign key target is SingleColumnCollection",
        messageFormat: "FK 타겟 컬럼 '{0}' (record '{1}')은 [SingleColumnCollection]이라 FK 대상이 될 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SwitchForeignKeyDuplicateConditionValue = new(
        id: "SDP0208",
        title: "SwitchForeignKey duplicate condition value",
        messageFormat: "record '{0}'의 파라미터 '{1}'에서 (ConditionColumn='{2}', ConditionValue='{3}') 조합이 중복됩니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SwitchForeignKeyConditionColumnNotFound = new(
        id: "SDP0209",
        title: "SwitchForeignKey condition column not found",
        messageFormat: "SwitchForeignKey의 조건 컬럼 '{0}'을 record '{1}'에서 찾을 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SwitchForeignKeyConditionColumnMismatch = new(
        id: "SDP0210",
        title: "SwitchForeignKey condition columns must be identical",
        messageFormat: "record '{0}'의 파라미터 '{1}'에 부착된 [SwitchForeignKey]들은 모두 같은 조건 컬럼을 사용해야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ForeignKeyColumnTypeMismatch = new(
        id: "SDP0211",
        title: "Foreign key column type does not match target column type",
        messageFormat: "FK 파라미터 '{0}' (type '{1}')의 타입이 타겟 컬럼 '{2}' (record '{3}', type '{4}')의 타입과 일치하지 않습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SwitchForeignKeyConditionValueInvalid = new(
        id: "SDP0212",
        title: "SwitchForeignKey condition value can never match the enum condition column",
        messageFormat: "record '{0}'의 [SwitchForeignKey] ConditionValue '{1}'은 조건 컬럼 '{2}' (enum '{3}')의 멤버명/숫자값과 일치하지 않아 런타임에 절대 매칭되지 않습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TableSetMemberMustBeStaticDataTable = new(
        id: "SDP0213",
        title: "TableSet member must be a StaticDataTable subclass",
        messageFormat: "TableSet 파라미터 '{0}' (type '{1}')은 StaticDataTable 서브클래스여야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ForeignKeyTargetColumnIsCollection = new(
        id: "SDP0214",
        title: "Foreign key target column is a collection type",
        messageFormat: "FK 타겟 컬럼 '{0}' (record '{1}')은 컬렉션 타입이라 FK 대상이 될 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TableRecordMustHaveStaticDataRecordAttribute = new(
        id: "SDP0215",
        title: "Table record type must have [StaticDataRecord]",
        messageFormat: "테이블 '{0}'의 record 타입 '{1}'에 [StaticDataRecord]가 부착되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TableSetMustBeRecordInCurrentCompilation = new(
        id: "SDP0216",
        title: "TableSet must be a record declared in the current compilation",
        messageFormat: "TableSet '{0}'은 현재 컴파일레이션에 선언된 record여야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TableMustBeDeclaredInCurrentCompilation = new(
        id: "SDP0217",
        title: "TableSet member table must be declared in the current compilation",
        messageFormat: "TableSet 멤버 '{0}'의 테이블 타입 '{1}'은 현재 컴파일레이션에 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ManagerTypeArgumentMustBeClosed = new(
        id: "SDP0218",
        title: "StaticDataManager type arguments must be closed types",
        messageFormat: "StaticDataManager를 상속한 '{0}'의 타입 인자가 타입 파라미터('{1}')입니다. 닫힌 타입으로 직접 상속해야 소스가 생성됩니다.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ManagerMustBePartial = new(
        id: "SDP0219",
        title: "StaticDataManager subclass must be partial",
        messageFormat: "StaticDataManager를 상속한 '{0}'은 partial로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewSetRecordMustBePartial = new(
        id: "SDP0301",
        title: "ViewSet record must be partial",
        messageFormat: "ViewSet record '{0}'은 partial로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewSetMemberMustBeNonNullable = new(
        id: "SDP0302",
        title: "ViewSet member must be non-nullable",
        messageFormat: "ViewSet 파라미터 '{0}' (type '{1}')은 non-nullable로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticDataViewMustBePartial = new(
        id: "SDP0303",
        title: "StaticDataView subclass must be partial",
        messageFormat: "StaticDataView를 상속한 '{0}'은 partial로 선언되어야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticDataViewMustHaveTableSetConstructor = new(
        id: "SDP0304",
        title: "StaticDataView subclass must have a single constructor accepting TTableSet",
        messageFormat: "StaticDataView '{0}'은 '{1}' 한 개를 받는 단일 생성자를 가져야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewSetMemberMustBeStaticDataView = new(
        id: "SDP0305",
        title: "ViewSet member must be a StaticDataView subclass",
        messageFormat: "ViewSet 파라미터 '{0}' (type '{1}')은 StaticDataView 서브클래스여야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewSetMustBeRecordInCurrentCompilation = new(
        id: "SDP0306",
        title: "ViewSet must be a record declared in the current compilation",
        messageFormat: "ViewSet '{0}'은 현재 컴파일레이션에 선언된 record여야 합니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewSetSharedByDifferentTableSets = new(
        id: "SDP0307",
        title: "ViewSet cannot be shared by managers with different TableSets",
        messageFormat: "ViewSet '{0}'은 서로 다른 TableSet('{1}', '{2}')을 사용하는 매니저들이 공유할 수 없습니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ViewTargetsDifferentTableSet = new(
        id: "SDP0308",
        title: "View must target the manager's TableSet",
        messageFormat: "ViewSet 파라미터 '{0}'의 View '{1}'은 TableSet '{2}'을(를) 대상으로 하지만, 매니저의 TableSet은 '{3}'입니다",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
