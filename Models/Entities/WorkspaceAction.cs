using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Models.Enums;

namespace Models.Entities;

public class WorkspaceAction : INotifyPropertyChanged
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }

    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public WorkspaceActionType ActionType { get; set; } = WorkspaceActionType.Command;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAtUtc { get; set; }

    public string LastRunText =>
        LastRunAtUtc is null
            ? "Never run"
            : $"Last run {LastRunAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm}";

    public ObservableCollection<WorkspaceLog> ActionRuns { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyDisplayChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Target));
        OnPropertyChanged(nameof(WorkingDirectory));
        OnPropertyChanged(nameof(ActionType));
        OnPropertyChanged(nameof(LastRunText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}