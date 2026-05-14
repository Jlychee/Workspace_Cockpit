using System.Collections.ObjectModel;

namespace Models;

public class WorkspaceItem
{
    public string Name { get; set; } = "";
    public string RootPath { get; set; } = "";
    public string Meta {get; set;} = "";
    public string ResumeText {get; set;} = "";
    
    public ObservableCollection<WorkspaceAction> Actions { get; set; } = [];
    public ObservableCollection<WorkspaceNote> Notes { get; set; } = [];
}