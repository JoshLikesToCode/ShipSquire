namespace ShipSquire.Domain.Enums;

public static class IncidentSeverity
{
    public const string Sev1 = "sev1";  // Critical - major outage
    public const string Sev2 = "sev2";  // High - significant impact
    public const string Sev3 = "sev3";  // Medium - limited impact
    public const string Sev4 = "sev4";  // Low - minor issue

    public static readonly string[] All = { Sev1, Sev2, Sev3, Sev4 };

    public static bool IsValid(string severity) => All.Contains(severity);
}
