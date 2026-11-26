using ExcelColumnExtractor.Exceptions;
using ExcelColumnExtractor.NameObjects;
using ExcelColumnExtractor.Scanners;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTest.InputFailureTests;

public class InputFailureTest
{
    [Fact]
    public void ThrownByExtractorCode_IsInputFailure_AndHasNoExceptionToLog()
    {
        var sheet = new ExcelSheetName("Book.xlsx", "Items");

        var scan = () => SheetHeaderScanner.Scan(sheet, "1A", NullLogger.Instance);

        var exception = Assert.Throws<ArgumentException>(scan);

        Assert.True(InputFailure.IsInputFailure(exception));
        Assert.Null(InputFailure.ExceptionForLog(exception));
    }

    [Fact]
    public void SameTypeThrownOutsideExtractorAndScanner_IsNotInputFailure()
    {
        var exception = Assert.Throws<ArgumentException>(ThrowArgumentException);

        Assert.False(InputFailure.IsInputFailure(exception));
        Assert.Same(exception, InputFailure.ExceptionForLog(exception));
    }

    [Fact]
    public void NullReference_IsNotInputFailure()
    {
        var exception = Assert.Throws<NullReferenceException>(DereferenceNull);

        Assert.False(InputFailure.IsInputFailure(exception));
        Assert.Same(exception, InputFailure.ExceptionForLog(exception));
    }

    [Fact]
    public void ExceptionNeverThrown_IsNotInputFailure()
    {
        var exception = new ArgumentException("테스트가 만들기만 하고 던지지 않은 예외");

        Assert.False(InputFailure.IsInputFailure(exception));
    }

    private static void ThrowArgumentException()
    {
        throw new ArgumentException("테스트 코드가 던진 예외");
    }

    private static void DereferenceNull()
    {
        string? text = null;
        _ = text!.Length;
    }
}
