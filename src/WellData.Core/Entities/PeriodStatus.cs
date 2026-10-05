namespace WellData.Core.Entities;

public enum PeriodStatus
{
    Open,

    /// <summary>Approved and locked. The month's data and issues can no longer change.</summary>
    Closed,
}
