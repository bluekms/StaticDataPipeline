using StaticDataHeaderGenerator;

namespace UnitTest.HeaderSeparatorValidatorTests;

public partial class HeaderSeparatorValidatorTest
{
    [Fact]
    public void NoConflict_DoesNotThrow()
    {
        var headers = new List<string> { "Id", "Name", "Price", "Category" };

        HeaderSeparatorValidator.Validate("ItemRecord", headers, "\t");
        HeaderSeparatorValidator.Validate("ItemRecord", headers, ",");
    }

    [Fact]
    public void SingleConflict_ThrowsInvalidOperationException()
    {
        var headers = new List<string> { "Id", "Sub,Total", "Price" };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            HeaderSeparatorValidator.Validate("ItemRecord", headers, ","));

        Assert.Contains("ItemRecord", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Sub,Total", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MultipleConflicts_AllAppearInMessage()
    {
        var headers = new List<string> { "A,B", "Id", "C,D", "Name" };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            HeaderSeparatorValidator.Validate("OrderRecord", headers, ","));

        Assert.Contains("A,B", exception.Message, StringComparison.Ordinal);
        Assert.Contains("C,D", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TabSeparator_ConflictDetected()
    {
        var headers = new List<string> { "Id", "Name\tinternal", "Price" };

        Assert.Throws<InvalidOperationException>(() =>
            HeaderSeparatorValidator.Validate("ItemRecord", headers, "\t"));
    }

    [Fact]
    public void MultiCharSeparator_ConflictDetected()
    {
        var headers = new List<string> { "Id", "Sub - Total", "Price" };

        Assert.Throws<InvalidOperationException>(() =>
            HeaderSeparatorValidator.Validate("ItemRecord", headers, " - "));
    }

    [Fact]
    public void MultiCharSeparator_NoConflict_Passes()
    {
        var headers = new List<string> { "Id", "Sub-Total", "Price" };

        HeaderSeparatorValidator.Validate("ItemRecord", headers, " - ");
    }

    [Fact]
    public void EmptySeparator_Passes()
    {
        var headers = new List<string> { "Id", "Sub,Total" };

        HeaderSeparatorValidator.Validate("ItemRecord", headers, string.Empty);
    }

    [Fact]
    public void EmptyHeaders_Passes()
    {
        var headers = new List<string>();

        HeaderSeparatorValidator.Validate("EmptyRecord", headers, ",");
    }

    [Fact]
    public void NestedColumnNameWithSeparatorInPrefix_ConflictDetected()
    {
        var headers = new List<string> { "Id", "Sub,Item.Id", "Sub,Item.Name" };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            HeaderSeparatorValidator.Validate("OrderRecord", headers, ","));

        Assert.Contains("Sub,Item.Id", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Sub,Item.Name", exception.Message, StringComparison.Ordinal);
    }
}
