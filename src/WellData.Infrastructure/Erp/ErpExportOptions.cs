namespace WellData.Infrastructure.Erp;

public sealed class ErpExportOptions
{
    public const string SectionName = "ErpExport";

    /// <summary>Folder export files are written to. A relative path is resolved against the content root.</summary>
    public string Directory { get; set; } = "exports";
}
