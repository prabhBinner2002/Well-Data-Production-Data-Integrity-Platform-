using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WellData.Api.Dtos;
using WellData.Core.Entities;
using WellData.Infrastructure.Periods;
using WellData.Infrastructure.Persistence;

namespace WellData.Api.Services;

public sealed class IssueService(WellDataDbContext context, PeriodGuard periodGuard)
{
    private static readonly Expression<Func<DataQualityIssue, IssueDto>> ToDto = issue =>
        new IssueDto(
            issue.Id,
            issue.RuleCode,
            issue.Rule.Description,
            issue.Rule.Severity,
            issue.Status,
            issue.ProductionMonth,
            issue.FacilityId,
            issue.WellId,
            issue.Field,
            issue.RawValue,
            issue.Message,
            issue.RowNumber,
            issue.DetectedAt,
            issue.ResolutionNote,
            issue.ModifiedBy,
            issue.ModifiedAt,
            issue.RowVersion);

    public Task<PagedResult<IssueDto>> ListAsync(IssueFilter filter, PageQuery page, CancellationToken cancellationToken)
    {
        var issues = context.DataQualityIssues.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.RuleCode))
        {
            issues = issues.Where(issue => issue.RuleCode == filter.RuleCode);
        }

        if (filter.Severity is { } severity)
        {
            issues = issues.Where(issue => issue.Rule.Severity == severity);
        }

        if (filter.Status is { } status)
        {
            issues = issues.Where(issue => issue.Status == status);
        }

        if (filter.Month is { } month)
        {
            issues = issues.Where(issue => issue.ProductionMonth == month);
        }

        if (!string.IsNullOrWhiteSpace(filter.WellId))
        {
            issues = issues.Where(issue => issue.WellId == filter.WellId);
        }

        return issues
            .OrderByDescending(issue => issue.ProductionMonth)
            .ThenBy(issue => issue.Id)
            .Select(ToDto)
            .ToPagedResultAsync(page, cancellationToken);
    }

    public Task<IssueDto?> GetAsync(long id, CancellationToken cancellationToken) =>
        context.DataQualityIssues
            .AsNoTracking()
            .Where(issue => issue.Id == id)
            .Select(ToDto)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<UpdateResult<IssueDto>> AcknowledgeAsync(long id, byte[] rowVersion, CancellationToken cancellationToken) =>
        UpdateAsync(id, rowVersion, issue => issue.Status = IssueStatus.Acknowledged, cancellationToken);

    public Task<UpdateResult<IssueDto>> ResolveAsync(long id, string note, byte[] rowVersion, CancellationToken cancellationToken) =>
        UpdateAsync(
            id,
            rowVersion,
            issue =>
            {
                issue.Status = IssueStatus.Resolved;
                issue.ResolutionNote = note.Trim();
            },
            cancellationToken);

    private async Task<UpdateResult<IssueDto>> UpdateAsync(
        long id,
        byte[] rowVersion,
        Action<DataQualityIssue> apply,
        CancellationToken cancellationToken)
    {
        var issue = await context.DataQualityIssues.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (issue is null)
        {
            return new UpdateResult<IssueDto>(UpdateOutcome.NotFound);
        }

        if (issue.Status == IssueStatus.Resolved)
        {
            return new UpdateResult<IssueDto>(UpdateOutcome.InvalidState, Detail: "A resolved issue cannot be acknowledged or resolved again.");
        }

        if (await periodGuard.IsClosedAsync(issue.ProductionMonth, cancellationToken))
        {
            return new UpdateResult<IssueDto>(UpdateOutcome.InvalidState, Detail: $"Period {issue.ProductionMonth} is closed.");
        }

        // EF puts the original RowVersion in the UPDATE's WHERE clause. Using the caller's value instead of the
        // one just read means the update only succeeds if nobody saved the row since the caller looked at it.
        context.Entry(issue).Property(candidate => candidate.RowVersion).OriginalValue = rowVersion;
        apply(issue);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new UpdateResult<IssueDto>(UpdateOutcome.ConcurrencyConflict);
        }

        return new UpdateResult<IssueDto>(UpdateOutcome.Updated, await GetAsync(id, cancellationToken));
    }
}
