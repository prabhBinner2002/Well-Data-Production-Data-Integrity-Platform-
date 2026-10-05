using System.IO.Compression;

namespace WellData.Infrastructure.Petrinex;

/// <summary>Unwraps a Petrinex download, which is usually a zip containing another zip containing the CSV.</summary>
public static class PetrinexArchive
{
    private const int MaxNestingDepth = 3;

    // Guards against a corrupt or hostile archive expanding without limit. The largest real file is about 300 MB.
    private const long MaxExtractedBytes = 4L * 1024 * 1024 * 1024;

    public static async Task ExtractCsvAsync(string downloadedPath, string destinationPath, CancellationToken cancellationToken)
    {
        var currentPath = downloadedPath;
        var temporaryPaths = new List<string>();

        try
        {
            for (var depth = 0; IsZip(currentPath); depth++)
            {
                if (depth == MaxNestingDepth)
                {
                    throw new InvalidDataException("Petrinex archive is nested more deeply than expected.");
                }

                var extractedPath = $"{destinationPath}.part{depth}";
                temporaryPaths.Add(extractedPath);
                await ExtractSingleEntryAsync(currentPath, extractedPath, cancellationToken);
                currentPath = extractedPath;
            }

            File.Copy(currentPath, destinationPath, overwrite: true);
        }
        finally
        {
            temporaryPaths.ForEach(File.Delete);
        }
    }

    private static async Task ExtractSingleEntryAsync(string zipPath, string extractedPath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.Entries.FirstOrDefault(candidate => candidate.Length > 0)
            ?? throw new InvalidDataException("Petrinex archive is empty.");

        if (entry.Length > MaxExtractedBytes)
        {
            throw new InvalidDataException($"Petrinex archive entry '{entry.Name}' is larger than the allowed size.");
        }

        await using var source = entry.Open();
        await using var target = File.Create(extractedPath);
        await source.CopyToAsync(target, cancellationToken);
    }

    private static bool IsZip(string path)
    {
        Span<byte> signature = stackalloc byte[2];
        using var stream = File.OpenRead(path);
        return stream.Read(signature) == 2 && signature[0] == (byte)'P' && signature[1] == (byte)'K';
    }
}
