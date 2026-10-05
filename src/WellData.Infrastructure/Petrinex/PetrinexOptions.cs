namespace WellData.Infrastructure.Petrinex;

public sealed class PetrinexOptions
{
    public const string SectionName = "Petrinex";

    public string BaseUrl { get; set; } = "https://www.petrinex.gov.ab.ca/publicdata/API/Files/AB/";

    /// <summary>Folder for downloaded files. A relative path is resolved against the content root.</summary>
    public string CacheDirectory { get; set; } = "data/cache";

    /// <summary>Operator BA IDs to load. An empty list loads every operator.</summary>
    public List<string> OperatorBaIds { get; set; } = [];
}
