using System.Reflection;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Schemata;

namespace ExcelColumnExtractor.Exceptions;

// 엑셀을 읽는 단계의 실패는 기획자가 보므로 스택 트레이스 없이 문구만 로그에 남긴다. 추출기 자체의 결함은 프로그래머가 보므로 남긴다.
// Record 를 읽는 단계와 옵션 확인에서 난 실패도 프로그래머가 보므로 Program 은 이 판정을 쓰지 않는다.
internal static class InputFailure
{
    private static readonly Type[] TypesThrownForInputFailure =
    [
        typeof(ArgumentException),
        typeof(ArgumentOutOfRangeException),
        typeof(InvalidOperationException),
        typeof(NotSupportedException),
        typeof(InvalidAttributeUsageException),
        typeof(IOException),
        typeof(EndOfStreamException),
    ];

    private static readonly Assembly[] ExtractorAndScannerAssemblies =
    [
        typeof(InputFailure).Assembly,
        typeof(RecordSchema).Assembly,
    ];

    public static bool IsInputFailure(Exception exception)
    {
        var isTypeThrownForInputFailure = Array.IndexOf(TypesThrownForInputFailure, exception.GetType()) >= 0;
        if (!isTypeThrownForInputFailure)
        {
            return false;
        }

        // 같은 타입이라도 런타임이나 다른 라이브러리 안에서 던져진 것은 추출기의 결함일 수 있다.
        var throwingAssembly = exception.TargetSite?.Module.Assembly;
        if (throwingAssembly is null)
        {
            return false;
        }

        return Array.IndexOf(ExtractorAndScannerAssemblies, throwingAssembly) >= 0;
    }

    public static Exception? ExceptionForLog(Exception exception)
    {
        var isInputFailure = IsInputFailure(exception);
        if (isInputFailure)
        {
            return null;
        }

        return exception;
    }
}
