using Sdp.Manager;

namespace UnitTest.StaticDataTests;

public class UnknownDisabledTableTests
{
    private static readonly string[] TableNames = ["ItemTable", "GachaTable"];

    [Fact]
    public void NoDisabledTables_DoesNotThrow()
    {
        TableSetLoaderHelper.ThrowIfUnknownDisabledTables(null, TableNames);
        TableSetLoaderHelper.ThrowIfUnknownDisabledTables([], TableNames);
    }

    [Fact]
    public void OnlyKnownNames_DoesNotThrow()
    {
        TableSetLoaderHelper.ThrowIfUnknownDisabledTables(["GachaTable", "ItemTable"], TableNames);
    }

    [Fact]
    public void UnknownNamesAmongKnownOnes_AreReportedInOneArgumentError()
    {
        List<string> disabledTables = ["itemTable", "GachaTable", "GachaTables"];

        var exception = Assert.Throws<ArgumentException>(
            () => TableSetLoaderHelper.ThrowIfUnknownDisabledTables(disabledTables, TableNames));

        Assert.Equal("disabledTables", exception.ParamName);
        Assert.Contains("'itemTable'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'ItemTable'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("'GachaTables'", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("'GachaTable'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SameUnknownNameTwice_IsReportedOnce()
    {
        List<string> disabledTables = ["GachaTables", "GachaTables"];

        var exception = Assert.Throws<ArgumentException>(
            () => TableSetLoaderHelper.ThrowIfUnknownDisabledTables(disabledTables, TableNames));

        var first = exception.Message.IndexOf("'GachaTables'", StringComparison.Ordinal);
        var last = exception.Message.LastIndexOf("'GachaTables'", StringComparison.Ordinal);
        Assert.True(first >= 0);
        Assert.Equal(first, last);
    }
}
