using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Petrinex;

namespace WellData.Tests.Integration.Infrastructure;

/// <summary>Serves fixture files in place of Petrinex so tests never touch the network.</summary>
public sealed class FixturePetrinexClient : IPetrinexClient
{
    private static readonly Dictionary<SourceFileType, string> InfrastructureFixtures = new()
    {
        [SourceFileType.BusinessAssociate] = FixtureFiles.BusinessAssociates,
        [SourceFileType.Facility] = FixtureFiles.Facilities,
        [SourceFileType.Well] = FixtureFiles.Wells,
        [SourceFileType.WellFacilityLink] = FixtureFiles.WellFacilityLinks,
    };

    private readonly Dictionary<ProductionMonth, string> _volumetricPaths = [];

    /// <summary>Makes a month available. Calling it again for the same month simulates Petrinex regenerating the file.</summary>
    public void SetVolumetricFile(ProductionMonth month, string localPath) => _volumetricPaths[month] = localPath;

    public Task<PetrinexFile> GetVolumetricFileAsync(ProductionMonth month, CancellationToken cancellationToken)
    {
        var url = $"https://petrinex.test/Vol/{month}/CSV";
        return _volumetricPaths.TryGetValue(month, out var path)
            ? Task.FromResult(new PetrinexFile(SourceFileType.Volumetric, month, url, path))
            : throw new PetrinexFileNotAvailableException(url);
    }

    public Task<PetrinexFile> GetInfrastructureFileAsync(SourceFileType fileType, CancellationToken cancellationToken) =>
        Task.FromResult(new PetrinexFile(
            fileType,
            Month: null,
            $"https://petrinex.test/Infra/{fileType}/CSV",
            FixtureFiles.PathTo(InfrastructureFixtures[fileType])));
}
