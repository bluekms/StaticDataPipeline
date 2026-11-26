using System.Globalization;
using System.Numerics;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;

// 정수 칸과 키 enum 칸이 같은 규칙으로 정수를 읽도록 한 곳에 둔다. 로더(Sdp 의 CsvMapperHelper)가 정수를 읽는 규칙과 같아야 한다.
internal static class IntegerCell
{
    public static bool TryParse<T>(string text, out T value)
        where T : struct, IBinaryInteger<T>
    {
        return T.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
