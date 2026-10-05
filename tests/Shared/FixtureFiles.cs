namespace WellData.Tests;

/// <summary>Locates the real-data snippet files copied from tests/fixtures into the test output folder.</summary>
public static class FixtureFiles
{
    public const string BusinessAssociates = "BusinessAssociate_sample.csv";
    public const string Facilities = "Facility_sample.csv";
    public const string Wells = "Well_sample.csv";
    public const string WellFacilityLinks = "WellFacilityLink_sample.csv";
    public const string Volumetric = "Vol_2026-06_sample.csv";

    /// <summary>The sample with one error injected per rule. See tests/fixtures/README.md for the list.</summary>
    public const string VolumetricWithErrors = "Vol_2026-06_with_errors.csv";

    /// <summary>The sample as if Petrinex regenerated it: two values changed, one unmasked, one row removed.</summary>
    public const string VolumetricRestated = "Vol_2026-06_restated.csv";

    public static string PathTo(string fileName) => Path.Combine(AppContext.BaseDirectory, "fixtures", fileName);

    public static StreamReader Open(string fileName) => new(PathTo(fileName));
}
