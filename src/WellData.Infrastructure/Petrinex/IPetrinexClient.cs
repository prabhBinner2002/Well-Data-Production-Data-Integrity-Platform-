using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Petrinex;

public interface IPetrinexClient
{
    /// <summary>Returns the month's volumetric CSV, downloading it only if it is not already cached.</summary>
    /// <exception cref="PetrinexFileNotAvailableException">Petrinex has not published the month.</exception>
    Task<PetrinexFile> GetVolumetricFileAsync(ProductionMonth month, CancellationToken cancellationToken);

    /// <summary>Returns an infrastructure (master data) CSV, downloading it only if it is not already cached.</summary>
    Task<PetrinexFile> GetInfrastructureFileAsync(SourceFileType fileType, CancellationToken cancellationToken);
}

/// <summary>A Petrinex CSV available on local disk.</summary>
public sealed record PetrinexFile(SourceFileType FileType, ProductionMonth? Month, string Url, string LocalPath);

public sealed class PetrinexFileNotAvailableException(string url)
    : Exception($"Petrinex has no file at {url}.");
