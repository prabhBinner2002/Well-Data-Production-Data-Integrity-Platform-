using Microsoft.EntityFrameworkCore;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Core.Validation;
using WellData.Core.Volumetric;
using WellData.Infrastructure.Persistence;

namespace WellData.Infrastructure.Ingestion.Volumetric;

/// <summary>
/// Validates a file's staged rows and replaces the month's VolumetricRecords with the rows that pass,
/// all in one transaction. Rows with an Error finding are quarantined: they stay in staging with an issue
/// explaining why, and never reach the typed table.
/// </summary>
public sealed class VolumetricPromoter(
    WellDataDbContext context,
    VolumetricRecordBulkWriter writer,
    ValidationEngine engine,
    TimeProvider timeProvider)
{
    private const int MaxRawValueLength = 150;

    public async Task<PromotionResult> PromoteAsync(SourceFile sourceFile, ProductionMonth month, CancellationToken cancellationToken)
    {
        var staged = await context.StagedVolumetricRows
            .AsNoTracking()
            .Where(row => row.SourceFileId == sourceFile.Id)
            .OrderBy(row => row.RowNumber)
            .ToListAsync(cancellationToken);

        var rows = staged.Select(row => new ParsedVolumetricRow(row)).ToList();
        var dataRows = rows.Where(row => !row.IsProrationHeader).ToList();

        var validation = engine.Validate(
            dataRows,
            await LoadValidationContextAsync(month, cancellationToken),
            await context.DataQualityRules.AsNoTracking().ToDictionaryAsync(rule => rule.Code, cancellationToken));

        // A row the rules let through can still be unloadable if a rule was disabled, so both checks apply.
        var accepted = dataRows
            .Where(row => !validation.IsRejected(row) && !row.HasUnreadableField && row.Month.Value == month)
            .ToList();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // Replacing the month keeps a reload of a regenerated file from doubling the data.
        await context.VolumetricRecords
            .Where(record => record.ProductionMonth == month)
            .ExecuteDeleteAsync(cancellationToken);

        var records = accepted
            .Select(row => ToRecord(row, sourceFile.Id, validation.HasWarning(row)))
            .ToList();
        await writer.WriteAsync(records, cancellationToken);

        await ReplaceFileIssuesAsync(sourceFile.Id, month, validation, cancellationToken);
        await SupersedeEarlierFilesAsync(sourceFile.Id, month, cancellationToken);

        sourceFile.Status = SourceFileStatus.Loaded;
        sourceFile.RowCount = staged.Count;
        sourceFile.LoadedAt = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new PromotionResult(
            RowsLoaded: records.Count,
            RowsRejected: dataRows.Count - accepted.Count,
            RowsSkipped: rows.Count - dataRows.Count);
    }

    private async Task<ValidationContext> LoadValidationContextAsync(ProductionMonth month, CancellationToken cancellationToken)
    {
        var activities = await context.ActivityCodes
            .AsNoTracking()
            .ToDictionaryAsync(activity => activity.Code, cancellationToken);

        var productCodes = await context.ProductCodes
            .AsNoTracking()
            .Select(product => product.Code)
            .ToHashSetAsync(cancellationToken);

        return new ValidationContext(month, activities, productCodes);
    }

    /// <summary>
    /// Open issues from an earlier version of this month's file describe rows that no longer exist, so they
    /// are replaced. Acknowledged and resolved issues are kept as a record of the review that was done.
    /// </summary>
    private async Task ReplaceFileIssuesAsync(int sourceFileId, ProductionMonth month, ValidationResult validation, CancellationToken cancellationToken)
    {
        await context.DataQualityIssues
            .Where(issue => issue.ProductionMonth == month)
            .Where(issue => issue.SourceFileId != null)
            .Where(issue => issue.Status == IssueStatus.Open)
            .ExecuteDeleteAsync(cancellationToken);

        var detectedAt = timeProvider.GetUtcNow().UtcDateTime;
        context.DataQualityIssues.AddRange(
            validation.Findings.Select(finding => ToIssue(finding, sourceFileId, month, detectedAt)));
    }

    private Task SupersedeEarlierFilesAsync(int sourceFileId, ProductionMonth month, CancellationToken cancellationToken) =>
        context.SourceFiles
            .Where(file => file.FileType == SourceFileType.Volumetric)
            .Where(file => file.ProductionMonth == month)
            .Where(file => file.Id != sourceFileId)
            .Where(file => file.Status == SourceFileStatus.Loaded)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(file => file.Status, SourceFileStatus.Superseded),
                cancellationToken);

    private static DataQualityIssue ToIssue(RuleFinding finding, int sourceFileId, ProductionMonth month, DateTime detectedAt)
    {
        var (row, field, rawValue, message) = finding.Violation;

        return new DataQualityIssue
        {
            RuleCode = finding.Rule.Code,
            ProductionMonth = month,
            SourceFileId = sourceFileId,
            RowNumber = row.RowNumber,
            FacilityId = NullIfEmpty(row.ReportingFacilityId),
            WellId = row.WellId,
            Field = field,
            RawValue = rawValue is { Length: > MaxRawValueLength } ? rawValue[..MaxRawValueLength] : rawValue,
            Message = message,
            Status = IssueStatus.Open,
            DetectedAt = detectedAt,
        };
    }

    private static VolumetricRecord ToRecord(ParsedVolumetricRow row, int sourceFileId, bool hasWarning) => new()
    {
        ProductionMonth = row.Month.Value!.Value,
        OperatorBaId = row.Staged.OperatorBaId?.Trim() ?? string.Empty,
        ReportingFacilityId = row.ReportingFacilityId,
        ActivityId = row.ActivityId,
        ProductId = NullIfEmpty(row.ProductId),
        FromToId = NullIfEmpty(row.FromToId),
        FromToIdType = NullIfEmpty(row.FromToIdType),
        Volume = row.Volume.Value,
        Energy = row.Energy.Value,
        Hours = row.Hours.Value,
        IsMasked = row.IsMasked,
        HasWarning = hasWarning,
        SourceFileId = sourceFileId,
        SourceRowNumber = row.RowNumber,
    };

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}
