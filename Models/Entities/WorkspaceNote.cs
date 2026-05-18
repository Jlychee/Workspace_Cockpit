namespace Models.Entities;

public static class WorkspaceNoteTypes
{
    public const string General = "General";
    public const string Runbook = "Runbook";
    public const string Idea = "Idea";
    public const string Todo = "Todo";

    public static readonly string[] All =
    [
        General,
        Runbook,
        Idea,
        Todo
    ];
}

public class WorkspaceNote
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Type { get; set; } = WorkspaceNoteTypes.General;
    public string Text { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string Header =>
        $"{Type} · Updated {UpdatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}";
}
