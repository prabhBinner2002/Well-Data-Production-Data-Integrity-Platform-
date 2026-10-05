using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WellData.Core;

namespace WellData.Infrastructure.Persistence;

/// <summary>Stores a <see cref="ProductionMonth"/> as a SQL date holding the first day of the month.</summary>
public sealed class ProductionMonthConverter : ValueConverter<ProductionMonth, DateOnly>
{
    public ProductionMonthConverter()
        : base(month => month.FirstDay, date => ProductionMonth.FromDate(date))
    {
    }
}
