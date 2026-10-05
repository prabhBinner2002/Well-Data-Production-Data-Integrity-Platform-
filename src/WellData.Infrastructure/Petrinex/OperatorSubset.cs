namespace WellData.Infrastructure.Petrinex;

/// <summary>The configured slice of operators to load. An empty subset includes everyone.</summary>
public sealed class OperatorSubset(IEnumerable<string> operatorBaIds)
{
    private readonly HashSet<string> _operatorBaIds = new(operatorBaIds, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> OperatorBaIds => _operatorBaIds;

    public bool Includes(string? operatorBaId) =>
        _operatorBaIds.Count == 0 || (operatorBaId is not null && _operatorBaIds.Contains(operatorBaId));
}
