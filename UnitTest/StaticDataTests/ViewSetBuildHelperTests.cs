using System.Globalization;
using Microsoft.Extensions.Logging;
using Sdp.View;
using UnitTest.Utility;
using Xunit.Abstractions;

namespace UnitTest.StaticDataTests;

public class ViewSetBuildHelperTests(ITestOutputHelper testOutputHelper)
{
    [Theory]
    [InlineData("en", "BadCtorView must have a constructor accepting TableSet.")]
    [InlineData("ko", "BadCtorView에는 TableSet을(를) 받는 생성자가 필요합니다.")]
    public void ViewConstructorNotFoundError_IsLocalized(string locale, string expected)
    {
        var logger = CreateLogger();

        var savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try
        {
            var ex = ViewSetBuildHelper.ViewConstructorNotFoundError("BadCtorView", "TableSet");
            Assert.Equal(expected, ex.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }

        Assert.Empty(logger.Logs);
    }

    [Theory]
    [InlineData("en", "Parameter 'Member' of type 'NullableMemberView' must be non-nullable; ViewSet members are always populated.")]
    [InlineData("ko", "'Member' 파라미터의 타입 'NullableMemberView'은(는) non-nullable이어야 합니다. ViewSet 멤버는 항상 빌더가 채웁니다.")]
    public void NullableViewMemberError_IsLocalized(string locale, string expected)
    {
        var logger = CreateLogger();

        var savedCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
        try
        {
            var ex = ViewSetBuildHelper.NullableViewMemberError("Member", "NullableMemberView");
            Assert.Equal(expected, ex.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = savedCulture;
        }

        Assert.Empty(logger.Logs);
    }

    private TestOutputLogger<ViewSetBuildHelperTests> CreateLogger()
    {
        var factory = new TestOutputLoggerFactory(testOutputHelper, LogLevel.Warning);
        if (factory.CreateLogger<ViewSetBuildHelperTests>() is not TestOutputLogger<ViewSetBuildHelperTests> logger)
        {
            throw new InvalidOperationException("Logger creation failed.");
        }

        return logger;
    }
}
