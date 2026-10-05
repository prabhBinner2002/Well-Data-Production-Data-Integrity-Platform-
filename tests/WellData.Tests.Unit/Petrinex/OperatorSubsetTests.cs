using WellData.Infrastructure.Petrinex;

namespace WellData.Tests.Unit.Petrinex;

public class OperatorSubsetTests
{
    [Fact]
    public void Configured_operators_are_included_and_others_are_not()
    {
        var subset = new OperatorSubset(["0026", "0XZ4"]);

        Assert.True(subset.Includes("0026"));
        Assert.True(subset.Includes("0xz4"));
        Assert.False(subset.Includes("0HE9"));
        Assert.False(subset.Includes(null));
    }

    [Fact]
    public void An_id_without_its_leading_zero_is_a_different_operator()
    {
        var subset = new OperatorSubset(["0026"]);

        Assert.False(subset.Includes("26"));
    }

    [Fact]
    public void Empty_subset_includes_every_operator()
    {
        var subset = new OperatorSubset([]);

        Assert.True(subset.Includes("0HE9"));
    }
}
