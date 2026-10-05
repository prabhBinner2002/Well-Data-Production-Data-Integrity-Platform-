namespace WellData.Core.Entities;

public enum SourceFileStatus
{
    Downloaded,
    Loaded,
    Failed,

    /// <summary>A newer version of the same month's file has been loaded since.</summary>
    Superseded,
}
