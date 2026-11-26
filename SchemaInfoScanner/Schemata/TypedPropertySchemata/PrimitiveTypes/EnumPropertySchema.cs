using System.Globalization;
using Microsoft.CodeAnalysis;
using SchemaInfoScanner.Extensions;
using SchemaInfoScanner.NameObjects;
using SchemaInfoScanner.Resources;
using SchemaInfoScanner.Schemata.AttributeCheckers;
using Sdp.Attributes;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;

public sealed record EnumPropertySchema(
    PropertyName PropertyName,
    INamedTypeSymbol NamedTypeSymbol,
    IReadOnlyList<AttributeInfo> AttributeList)
    : PropertySchemaBase(PropertyName, NamedTypeSymbol, AttributeList)
{
    protected override void OnCheckCompatibility(CompatibilityContext context)
    {
        var cell = context.Consume();
        var value = cell.Value;

        var isMember = NamedTypeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(x => x.IsConst)
            .Any(x => x.Name == value);

        if (!isMember)
        {
            // 키 enum 칸은 enum 에 없는 숫자도 받는다. 로더가 기반 정수 타입으로 읽어 캐스팅하므로 그 타입으로 읽히는 값만 통과시킨다.
            var isKey = AttributeAccessors.HasAttribute<KeyAttribute>(AttributeList);
            var isAccepted = isKey && IsUnderlyingInteger(NamedTypeSymbol, value);
            if (!isAccepted)
            {
                var format = isKey
                    ? Messages.Composite.InvalidCellValueForKeyEnum
                    : Messages.Composite.InvalidCellValueForEnum;
                var enumName = new EnumName(NamedTypeSymbol);
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    format,
                    cell.Value,
                    cell.Address,
                    enumName.FullName));
            }
        }

        if (this.HasAttribute<RangeAttribute>())
        {
            RangeAttributeChecker.CheckEnum(this, value, NamedTypeSymbol);
        }

        context.Collect(value);
    }

    private static bool IsUnderlyingInteger(INamedTypeSymbol enumType, string value)
    {
        switch (enumType.EnumUnderlyingType?.SpecialType)
        {
            case SpecialType.System_SByte:
                return IntegerCell.TryParse<sbyte>(value, out _);
            case SpecialType.System_Byte:
                return IntegerCell.TryParse<byte>(value, out _);
            case SpecialType.System_Int16:
                return IntegerCell.TryParse<short>(value, out _);
            case SpecialType.System_UInt16:
                return IntegerCell.TryParse<ushort>(value, out _);
            case SpecialType.System_UInt32:
                return IntegerCell.TryParse<uint>(value, out _);
            case SpecialType.System_Int64:
                return IntegerCell.TryParse<long>(value, out _);
            case SpecialType.System_UInt64:
                return IntegerCell.TryParse<ulong>(value, out _);
            default:
                return IntegerCell.TryParse<int>(value, out _);
        }
    }
}
