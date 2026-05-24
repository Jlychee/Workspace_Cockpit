namespace Models.Entities;

public class WorkspaceLog
{
    public int Id { get; set; }

    public int WorkspaceId { get; set; }
    public int WorkspaceActionId { get; set; }

    public string ActionNameSnapshot { get; set; } = "";
    public string TargetSnapshot { get; set; } = "";

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }

    public string Status { get; set; } = "Started";
    public int? ExitCode { get; set; }
    public long? DurationMs { get; set; }

    public string OutputPreview { get; set; } = "";
    public string ErrorMessage { get; set; } = "";

    public string StartedText => StartedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss");

    public string DurationText =>
        DurationMs is null
            ? ""
            : $"{DurationMs} ms";

    public string ExitCodeText =>
        ExitCode is null
            ? ""
            : $"exit {ExitCode}";
}