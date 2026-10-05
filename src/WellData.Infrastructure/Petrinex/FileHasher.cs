using System.Security.Cryptography;

namespace WellData.Infrastructure.Petrinex;

public static class FileHasher
{
    /// <summary>Returns the file's SHA-256 hash as 64 lowercase hex characters.</summary>
    public static async Task<string> Sha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(hash);
    }
}
