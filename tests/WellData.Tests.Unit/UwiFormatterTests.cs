using WellData.Core;

namespace WellData.Tests.Unit;

public class UwiFormatterTests
{
    [Fact]
    public void Sixteen_character_identifier_is_formatted_as_a_UWI()
    {
        Assert.Equal("100/06-12-045-05W5/00", UwiFormatter.Format("100061204505W500"));
    }

    [Fact]
    public void Leading_zeros_are_preserved()
    {
        Assert.Equal("100/01-01-001-15W4/02", UwiFormatter.Format("100010100115W402"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("***")]
    [InlineData("1000612045055500")]
    public void Unrecognised_identifiers_are_returned_unchanged(string identifier)
    {
        Assert.Equal(identifier, UwiFormatter.Format(identifier));
    }
}
