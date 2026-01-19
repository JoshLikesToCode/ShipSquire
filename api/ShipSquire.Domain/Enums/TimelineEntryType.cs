namespace ShipSquire.Domain.Enums;

public static class TimelineEntryType
{
    public const string Note = "note";
    public const string Action = "action";
    public const string Decision = "decision";
    public const string Observation = "observation";

    public static readonly string[] All = { Note, Action, Decision, Observation };

    public static bool IsValid(string entryType) => All.Contains(entryType);
}
