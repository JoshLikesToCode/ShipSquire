namespace ShipSquire.Domain.Enums;

public static class IncidentStatus
{
    public const string Open = "open";
    public const string Investigating = "investigating";
    public const string Mitigated = "mitigated";
    public const string Resolved = "resolved";

    public static readonly string[] All = { Open, Investigating, Mitigated, Resolved };

    public static bool IsValid(string status) => All.Contains(status);

    /// <summary>
    /// Valid status transitions:
    /// open → investigating
    /// investigating → mitigated, resolved
    /// mitigated → resolved, investigating (reopen if issue recurs)
    /// resolved → open (reopen if issue recurs)
    /// </summary>
    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        { Open, new[] { Investigating } },
        { Investigating, new[] { Mitigated, Resolved } },
        { Mitigated, new[] { Resolved, Investigating } },
        { Resolved, new[] { Open } }
    };

    public static bool CanTransition(string from, string to)
    {
        if (!IsValid(from) || !IsValid(to)) return false;
        if (from == to) return false;
        return ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static string[] GetValidTransitions(string from)
    {
        if (!IsValid(from)) return Array.Empty<string>();
        return ValidTransitions.TryGetValue(from, out var allowed) ? allowed : Array.Empty<string>();
    }
}
