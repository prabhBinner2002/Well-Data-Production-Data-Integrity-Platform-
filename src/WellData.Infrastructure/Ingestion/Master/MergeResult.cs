namespace WellData.Infrastructure.Ingestion.Master;

/// <summary>How many target rows a MERGE inserted and updated. Both are zero when nothing changed.</summary>
public sealed record MergeResult(int Inserted, int Updated)
{
    public bool ChangedAnything => Inserted > 0 || Updated > 0;
}
