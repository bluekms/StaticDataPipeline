using System.Globalization;
using System.Numerics;

namespace SchemaInfoScanner.Schemata.TypedPropertySchemata.PrimitiveTypes;

internal static class IntegerCell
{
    public static bool TryParse<T>(string text, out T value)
        where T : struct, IBinaryInteger<T>
    {
        return T.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
