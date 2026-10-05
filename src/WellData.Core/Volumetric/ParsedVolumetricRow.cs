using WellData.Core.Entities;

namespace WellData.Core.Volumetric;

/// <summary>A staged row with its typed fields parsed, ready for validation and promotion.</summary>
public sealed class ParsedVolumetricRow
{
    private const string WellIdType = "WI";

    public ParsedVolumetricRow(StagedVolumetricRow staged)
    {
        Staged = staged;
        ActivityId = Clean(staged.ActivityId);
        ProductId = Clean(staged.ProductId);
        FromToId = Clean(staged.FromToId);
        FromToIdType = Clean(staged.FromToIdType);
        ReportingFacilityId = Clean(staged.ReportingFacilityId);
        Month = ParsedField.Month(staged.ProductionMonth);
        Volume = ParsedField.Decimal(staged.Volume);
        Energy = ParsedField.Decimal(staged.Energy);
        Hours = ParsedField.Integer(staged.Hours);
    }

    public StagedVolumetricRow Staged { get; }

    public int RowNumber => Staged.RowNumber;

    public string ReportingFacilityId { get; }

    public string ActivityId { get; }

    public string ProductId { get; }

    public string FromToId { get; }

    public string FromToIdType { get; }

    public ParsedField<ProductionMonth> Month { get; }

    public ParsedField<decimal> Volume { get; }

    public ParsedField<decimal> Energy { get; }

    public ParsedField<int> Hours { get; }

    /// <summary>
    /// Rows with no activity and no product are facility proration factor headers.
    /// They are part of the file format, not volumetric data, and are never errors.
    /// </summary>
    public bool IsProrationHeader => ActivityId.Length == 0 && ProductId.Length == 0;

    public bool IsFromToMasked => ParsedField.IsMasked(FromToId);

    public bool IsMasked =>
        IsFromToMasked || Volume.State == FieldState.Masked || Hours.State == FieldState.Masked;

    /// <summary>The well this row is about, or null when the From/To side is a facility, masked, or absent.</summary>
    public string? WellId => FromToIdType == WellIdType && !IsFromToMasked ? FromToId : null;

    public bool HasUnreadableField =>
        !Month.IsValid ||
        Volume.State == FieldState.Invalid ||
        Energy.State == FieldState.Invalid ||
        Hours.State == FieldState.Invalid ||
        HasOversizedIdentifier;

    /// <summary>True when an ID is longer than Petrinex ever issues, which means the row is corrupt.</summary>
    public bool HasOversizedIdentifier =>
        Clean(Staged.OperatorBaId).Length > 4 ||
        ReportingFacilityId.Length > 20 ||
        FromToId.Length > 20 ||
        FromToIdType.Length > 2;

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;
}
