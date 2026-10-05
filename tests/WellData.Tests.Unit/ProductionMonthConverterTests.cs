using WellData.Core;
using WellData.Infrastructure.Persistence;

namespace WellData.Tests.Unit;

public class ProductionMonthConverterTests
{
    private readonly ProductionMonthConverter _converter = new();

    [Fact]
    public void Month_is_stored_as_the_first_day_of_the_month()
    {
        var stored = (DateOnly)_converter.ConvertToProvider(new ProductionMonth(2026, 6))!;

        Assert.Equal(new DateOnly(2026, 6, 1), stored);
    }

    [Fact]
    public void Stored_date_is_read_back_as_its_month_even_when_not_the_first_day()
    {
        var month = (ProductionMonth)_converter.ConvertFromProvider(new DateOnly(2026, 6, 17))!;

        Assert.Equal(new ProductionMonth(2026, 6), month);
    }

    [Fact]
    public void Month_round_trips_through_the_converter()
    {
        var original = new ProductionMonth(2024, 2);

        var stored = _converter.ConvertToProvider(original);
        var restored = (ProductionMonth)_converter.ConvertFromProvider(stored)!;

        Assert.Equal(original, restored);
    }
}
