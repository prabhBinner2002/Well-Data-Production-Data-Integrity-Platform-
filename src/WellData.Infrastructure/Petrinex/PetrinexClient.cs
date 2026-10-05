using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellData.Core;
using WellData.Core.Entities;

namespace WellData.Infrastructure.Petrinex;

/// <summary>Downloads Petrinex public data files and keeps them in a local cache.</summary>
public sealed class PetrinexClient(HttpClient httpClient, IOptions<PetrinexOptions> options, ILogger<PetrinexClient> logger)
    : IPetrinexClient
{
    private static readonly Dictionary<SourceFileType, string> InfrastructureFileNames = new()
    {
        [SourceFileType.BusinessAssociate] = "Business Associate",
        [SourceFileType.Facility] = "Facility Infrastructure",
        [SourceFileType.Well] = "Well Infrastructure",
        [SourceFileType.WellFacilityLink] = "Well to Facility Link",
    };

    private readonly PetrinexOptions _options = options.Value;

    public Task<PetrinexFile> GetVolumetricFileAsync(ProductionMonth month, CancellationToken cancellationToken) =>
        GetFileAsync(SourceFileType.Volumetric, month, $"Vol/{month}/CSV", $"Vol_{month}-AB.CSV", cancellationToken);

    public Task<PetrinexFile> GetInfrastructureFileAsync(SourceFileType fileType, CancellationToken cancellationToken)
    {
        if (!InfrastructureFileNames.TryGetValue(fileType, out var name))
        {
            throw new ArgumentException($"{fileType} is not an infrastructure file.", nameof(fileType));
        }

        var relativeUrl = $"Infra/{Uri.EscapeDataString(name)}/CSV";
        var cacheFileName = $"Infra_{name.Replace(" ", string.Empty)}-AB.CSV";
        return GetFileAsync(fileType, month: null, relativeUrl, cacheFileName, cancellationToken);
    }

    private async Task<PetrinexFile> GetFileAsync(
        SourceFileType fileType,
        ProductionMonth? month,
        string relativeUrl,
        string cacheFileName,
        CancellationToken cancellationToken)
    {
        var url = new Uri(new Uri(_options.BaseUrl), relativeUrl);
        var cachePath = Path.Combine(_options.CacheDirectory, cacheFileName);
        var file = new PetrinexFile(fileType, month, url.ToString(), cachePath);

        if (File.Exists(cachePath))
        {
            return file;
        }

        Directory.CreateDirectory(_options.CacheDirectory);
        var downloadPath = $"{cachePath}.download";

        try
        {
            logger.LogInformation("Downloading {Url}", url);
            await DownloadAsync(url, downloadPath, cancellationToken);
            await PetrinexArchive.ExtractCsvAsync(downloadPath, cachePath, cancellationToken);
            logger.LogInformation("Cached {FileName}", cacheFileName);
            return file;
        }
        finally
        {
            File.Delete(downloadPath);
        }
    }

    private async Task DownloadAsync(Uri url, string downloadPath, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new PetrinexFileNotAvailableException(url.ToString());
        }

        response.EnsureSuccessStatusCode();

        await using var target = File.Create(downloadPath);
        await response.Content.CopyToAsync(target, cancellationToken);
    }
}
