using Microsoft.EntityFrameworkCore;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Auditing;
using WellData.Infrastructure.Persistence;

namespace WellData.Tests.Unit.Auditing;

public class AuditSaveChangesInterceptorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 15, 30, 0, TimeSpan.Zero);

    private readonly AuditSaveChangesInterceptor _interceptor = new(new NamedUser("steward"), new ManualTimeProvider(Now));

    [Fact]
    public void Added_entity_gets_created_fields_only()
    {
        using var context = CreateContext();
        var issue = NewIssue();
        context.Add(issue);

        _interceptor.Stamp(context);

        Assert.Equal(Now.UtcDateTime, issue.CreatedAt);
        Assert.Equal("steward", issue.CreatedBy);
        Assert.Null(issue.ModifiedAt);
        Assert.Null(issue.ModifiedBy);
    }

    [Fact]
    public void Modified_entity_gets_modified_fields_and_keeps_its_created_fields()
    {
        using var context = CreateContext();
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var issue = NewIssue();
        issue.Id = 42;
        issue.CreatedAt = created;
        issue.CreatedBy = "system";
        context.Attach(issue);
        issue.Status = IssueStatus.Resolved;

        _interceptor.Stamp(context);

        Assert.Equal(Now.UtcDateTime, issue.ModifiedAt);
        Assert.Equal("steward", issue.ModifiedBy);
        Assert.Equal(created, issue.CreatedAt);
        Assert.Equal("system", issue.CreatedBy);
    }

    [Fact]
    public void Unchanged_entity_is_left_alone()
    {
        using var context = CreateContext();
        var issue = NewIssue();
        issue.Id = 42;
        context.Attach(issue);

        _interceptor.Stamp(context);

        Assert.Null(issue.CreatedAt);
        Assert.Null(issue.ModifiedAt);
    }

    [Fact]
    public void Entities_that_are_not_auditable_are_ignored()
    {
        using var context = CreateContext();
        context.Add(new BusinessAssociate { BaId = "0026", LegalName = "OVINTIV CANADA ULC" });

        var exception = Record.Exception(() => _interceptor.Stamp(context));

        Assert.Null(exception);
    }

    private static DataQualityIssue NewIssue() => new()
    {
        RuleCode = "REF-01",
        ProductionMonth = new ProductionMonth(2026, 6),
        Message = "Well is not in master data.",
    };

    /// <summary>The change tracker works without a database, so no connection is ever opened here.</summary>
    private static WellDataDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<WellDataDbContext>().UseSqlServer("Server=not-used").Options);

    private sealed class NamedUser(string name) : ICurrentUser
    {
        public string Name => name;
    }
}
