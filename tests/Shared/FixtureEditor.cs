namespace WellData.Tests;

/// <summary>Makes a temporary copy of a real fixture file with deliberate changes, for error and restatement tests.</summary>
public static class FixtureEditor
{
    /// <param name="edit">Receives every line including the header and returns the lines to write.</param>
    public static string CopyWith(string fixtureFileName, Func<List<string>, IEnumerable<string>> edit)
    {
        var lines = File.ReadAllLines(FixtureFiles.PathTo(fixtureFileName)).ToList();
        var path = Path.Combine(Path.GetTempPath(), $"welldata-fixture-{Guid.NewGuid():N}.csv");
        File.WriteAllLines(path, edit(lines));
        return path;
    }

    /// <summary>Rewrites the first line containing <paramref name="find"/>, failing loudly if the fixture no longer has it.</summary>
    public static IEnumerable<string> ReplaceInFirstMatch(this List<string> lines, string find, string replacement)
    {
        var index = lines.FindIndex(line => line.Contains(find, StringComparison.Ordinal));
        if (index < 0)
        {
            throw new InvalidOperationException($"Fixture has no line containing '{find}'.");
        }

        lines[index] = lines[index].Replace(find, replacement, StringComparison.Ordinal);
        return lines;
    }
}
