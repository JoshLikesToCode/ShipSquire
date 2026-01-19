namespace ShipSquire.Domain.Enums;

public static class IncidentStatus
{
    public const string Open = "open";
    public const string Investigating = "investigating";
    public const string Mitigated = "mitigated";
    public const string Resolved = "resolved";

    public static readonly string[] All = { Open, Investigating, Mitigated, Resolved };

    public static bool IsValid(string status) => All.Contains(status);
}
