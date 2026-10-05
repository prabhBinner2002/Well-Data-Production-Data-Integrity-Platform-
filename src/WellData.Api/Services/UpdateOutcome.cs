namespace WellData.Api.Services;

public enum UpdateOutcome
{
    Updated,
    NotFound,

    /// <summary>The row was changed by someone else after the caller read it.</summary>
    ConcurrencyConflict,

    /// <summary>The row's current state does not allow this change.</summary>
    InvalidState,
}

/// <param name="Value">The updated resource when the outcome is Updated.</param>
/// <param name="Detail">Explains an InvalidState outcome.</param>
public sealed record UpdateResult<T>(UpdateOutcome Outcome, T? Value = null, string? Detail = null)
    where T : class;
