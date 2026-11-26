using System.Reflection;
using SchemaInfoScanner.Exceptions;
using SchemaInfoScanner.Schemata;

namespace ExcelColumnExtractor.Exceptions;

// 엑셀을 읽는 단계의 실패는 기획자가 본다. 엑셀을 고치면 사라지는 실패는 문구만 남기고, 추출기 자체의 결함에는 스택 트레이스를 남긴다.
// Record 를 읽는 단계와 옵션 확인에서 난 실패는 프로그래머가 보므로 이 판정을 거치지 않고 Program 에서 스택 트레이스와 함께 남긴다.
// 엑셀을 읽는 단계에서 확인하는 선언의 실수(시작 셀 특성 등)는 문구에 Record 이름이 있어 문구만 남긴다.
// 추출기와 스캐너의 코드가 아래 타입으로 직접 던진 예외만 입력 실패로 본다.
// 같은 타입이라도 런타임이나 다른 라이브러리 안에서 던져진 것은 결함일 수 있다.
internal static class InputFailure
{
    private static readonly Type[] ThrownTypes =
    [
        typeof(ArgumentException),
        typeof(ArgumentOutOfRangeException),
        typeof(InvalidOperationException),
        typeof(NotSupportedException),
        typeof(InvalidAttributeUsageException),
        typeof(IOException),
        typeof(EndOfStreamException),
    ];

    private static readonly Assembly[] OwnAssemblies =
    [
        typeof(InputFailure).Assembly,
        typeof(RecordSchema).Assembly,
    ];

    public static bool IsInputFailure(Exception exception)
    {
        var isThrownType = Array.IndexOf(ThrownTypes, exception.GetType()) >= 0;
        if (!isThrownType)
        {
            return false;
        }

        var thrownFrom = exception.TargetSite?.Module.Assembly;
        if (thrownFrom is null)
        {
            return false;
        }

        return Array.IndexOf(OwnAssemblies, thrownFrom) >= 0;
    }

    // 로그에 함께 넘길 예외. 입력 실패는 문구만 남기도록 null 을 돌려준다.
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
