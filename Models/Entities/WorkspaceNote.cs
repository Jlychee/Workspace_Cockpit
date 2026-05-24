using System.ComponentModel;
using System.Runtime.CompilerServices;

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

public class WorkspaceNote : INotifyPropertyChanged
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public string Type { get; set; } = WorkspaceNoteTypes.General;
    public string Text { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string UpdatedText =>
        $"Updated {UpdatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string Header =>
        $"{Type} - {UpdatedText}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyDisplayChanged()
    {
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(Type));
        OnPropertyChanged(nameof(UpdatedAtUtc));
        OnPropertyChanged(nameof(UpdatedText));
        OnPropertyChanged(nameof(Header));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}