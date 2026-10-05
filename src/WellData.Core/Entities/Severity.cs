namespace WellData.Core.Entities;

public enum Severity
{
    /// <summary>Worth recording, nothing is wrong (for example a value masked for confidentiality).</summary>
    Info,

    /// <summary>Suspicious. The row is still loaded, flagged for review.</summary>
    Warning,

    /// <summary>The row cannot be trusted and is kept out of the loaded data.</summary>
    Error,
}
