using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Persistence.Configurations;

internal static class AuditableConfiguration
{
    /// <summary>Column sizes for the audit fields, shared by every auditable entity's configuration.</summary>
    public static void ConfigureAuditFields<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(100);
        builder.Property(entity => entity.ModifiedBy).HasMaxLength(100);
    }
}
