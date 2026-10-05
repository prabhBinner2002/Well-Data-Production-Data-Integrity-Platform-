namespace WellData.Core;

/// <summary>Converts Petrinex's 16-character well identifier into the UWI display format.</summary>
public static class UwiFormatter
{
    private const int DlsIdentifierLength = 16;

    /// <summary>Turns "100061204505W500" into "100/06-12-045-05W5/00". Other shapes are returned unchanged.</summary>
    public static string Format(string wellIdentifier)
    {
        if (wellIdentifier.Length != DlsIdentifierLength || wellIdentifier[12] != 'W')
        {
            return wellIdentifier;
        }

        var locationException = wellIdentifier[..3];
        var legalSubdivision = wellIdentifier[3..5];
        var section = wellIdentifier[5..7];
        var township = wellIdentifier[7..10];
        var range = wellIdentifier[10..12];
        var meridian = wellIdentifier[12..14];
        var eventSequence = wellIdentifier[14..16];

        return $"{locationException}/{legalSubdivision}-{section}-{township}-{range}{meridian}/{eventSequence}";
    }
}
