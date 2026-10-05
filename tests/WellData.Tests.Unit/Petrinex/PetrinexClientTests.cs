using System.IO.Compression;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WellData.Core;
using WellData.Core.Entities;
using WellData.Infrastructure.Petrinex;

namespace WellData.Tests.Unit.Petrinex;

public sealed class PetrinexClientTests : IDisposable
{
    private const string Csv = "ProductionMonth,OperatorBAID\n2026-06,0026\n";
    private readonly string _cacheDirectory = Path.Combine(Path.GetTempPath(), $"petrinex-tests-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Doubly_zipped_download_is_cached_as_a_plain_csv()
    {
        var handler = new StubHandler(_ => ZipResponse(Zip("Vol_2026-06-AB.zip", Zip("Vol_2026-06-AB.CSV", Encoding.UTF8.GetBytes(Csv)))));

        var file = await CreateClient(handler).GetVolumetricFileAsync(new ProductionMonth(2026, 6), CancellationToken.None);

        Assert.Equal(Csv, await File.ReadAllTextAsync(file.LocalPath));
        Assert.Equal("Vol_2026-06-AB.CSV", Path.GetFileName(file.LocalPath));
        Assert.Equal("https://petrinex.test/Files/AB/Vol/2026-06/CSV", file.Url);
        Assert.Equal([Path.GetFileName(file.LocalPath)], Directory.GetFiles(_cacheDirectory).Select(Path.GetFileName));
    }

    [Fact]
    public async Task Unzipped_response_is_cached_as_is()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Csv) });

        var file = await CreateClient(handler).GetVolumetricFileAsync(new ProductionMonth(2026, 6), CancellationToken.None);

        Assert.Equal(Csv, await File.ReadAllTextAsync(file.LocalPath));
    }

    [Fact]
    public async Task Cached_file_is_never_downloaded_again()
    {
        var handler = new StubHandler(_ => ZipResponse(Zip("Vol_2026-06-AB.CSV", Encoding.UTF8.GetBytes(Csv))));
        var client = CreateClient(handler);

        await client.GetVolumetricFileAsync(new ProductionMonth(2026, 6), CancellationToken.None);
        await client.GetVolumetricFileAsync(new ProductionMonth(2026, 6), CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task Infrastructure_file_name_is_escaped_in_the_url()
    {
        var handler = new StubHandler(_ => ZipResponse(Zip("Business Associate-AB.CSV", Encoding.UTF8.GetBytes(Csv))));

        var file = await CreateClient(handler).GetInfrastructureFileAsync(SourceFileType.BusinessAssociate, CancellationToken.None);

        Assert.Equal("https://petrinex.test/Files/AB/Infra/Business%20Associate/CSV", handler.LastRequestUri!.AbsoluteUri);
        Assert.Equal("Infra_BusinessAssociate-AB.CSV", Path.GetFileName(file.LocalPath));
    }

    [Fact]
    public async Task Unpublished_month_throws_and_leaves_nothing_in_the_cache()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<PetrinexFileNotAvailableException>(() =>
            CreateClient(handler).GetVolumetricFileAsync(new ProductionMonth(2030, 1), CancellationToken.None));

        Assert.Empty(Directory.GetFiles(_cacheDirectory));
    }

    private PetrinexClient CreateClient(HttpMessageHandler handler)
    {
        var options = Options.Create(new PetrinexOptions
        {
            BaseUrl = "https://petrinex.test/Files/AB/",
            CacheDirectory = _cacheDirectory,
        });

        return new PetrinexClient(new HttpClient(handler), options, NullLogger<PetrinexClient>.Instance);
    }

    private static HttpResponseMessage ZipResponse(byte[] zip) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(zip) };

    private static byte[] Zip(string entryName, byte[] content)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var entry = archive.CreateEntry(entryName).Open();
            entry.Write(content);
        }

        return buffer.ToArray();
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri;
            return Task.FromResult(respond(request));
        }
    }
}
