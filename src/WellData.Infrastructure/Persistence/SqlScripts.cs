using System.Reflection;

namespace WellData.Infrastructure.Persistence;

/// <summary>Loads the .sql files from the repository's /sql folder, which are embedded into this assembly.</summary>
public static class SqlScripts
{
    private static readonly Assembly Assembly = typeof(SqlScripts).Assembly;

    /// <param name="relativePath">Path under /sql using forward slashes, for example "rules/DUP-01.sql".</param>
    public static string Load(string relativePath)
    {
        // MSBuild writes RecursiveDir with the build machine's separator, so accept either form.
        var resourceName = Assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.Replace('\\', '/').Equals($"Sql/{relativePath}", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"Embedded SQL script '{relativePath}' was not found.");

        using var stream = Assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
