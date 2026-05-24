using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Models.Entities;

public class WorkspaceItem : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string RootPath { get; set; } = "";
    public string ResumeText { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastOpenedAtUtc { get; set; }

    public string Meta => $"{UpdatedText}";

    public string CreatedText =>
        $"Created {CreatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string UpdatedText =>
        $"Updated {UpdatedAtUtc.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string LastOpenedText =>
        LastOpenedAtUtc is null
            ? "Last opened never"
            : $"Last opened {LastOpenedAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm}";

    public string DetailsText => $"{CreatedText} · {UpdatedText} · {LastOpenedText}";

    public ObservableCollection<WorkspaceAction> Actions { get; set; } = [];
    public ObservableCollection<WorkspaceNote> Notes { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyDisplayChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(RootPath));
        OnPropertyChanged(nameof(ResumeText));
        OnPropertyChanged(nameof(Meta));
        OnPropertyChanged(nameof(CreatedText));
        OnPropertyChanged(nameof(UpdatedText));
        OnPropertyChanged(nameof(LastOpenedText));
        OnPropertyChanged(nameof(DetailsText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}