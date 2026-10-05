using Microsoft.EntityFrameworkCore;
using WellData.Core.Entities;
using WellData.Infrastructure.Persistence;
using WellData.Infrastructure.Petrinex;

namespace WellData.Infrastructure.Ingestion;

/// <summary>Records each distinct version of a Petrinex file, identified by type, month, and content hash.</summary>
public sealed class SourceFileRegistry(WellDataDbContext context, TimeProvider timeProvider)
{
    /// <summary>Returns the tracked SourceFile for this exact content, creating it if it has not been seen before.</summary>
    public async Task<SourceFile> GetOrRegisterAsync(PetrinexFile file, CancellationToken cancellationToken)
    {
        var hash = await FileHasher.Sha256Async(file.LocalPath, cancellationToken);

        var existing = await context.SourceFiles
            .Where(source => source.FileType == file.FileType)
            .Where(source => source.ProductionMonth == file.Month)
            .Where(source => source.Sha256 == hash)
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var sourceFile = new SourceFile
        {
            Url = file.Url,
            FileName = Path.GetFileName(file.LocalPath),
            FileType = file.FileType,
            ProductionMonth = file.Month,
            Sha256 = hash,
            Status = SourceFileStatus.Downloaded,
            DownloadedAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        context.SourceFiles.Add(sourceFile);
        await context.SaveChangesAsync(cancellationToken);
        return sourceFile;
    }
}
