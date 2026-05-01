using System.Windows;
using System.Windows.Controls;

namespace Workspace_Cockpit;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private void AddLog(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        ExecutionLog.Items.Insert(0, $"{time} {message}");
    }

    public MainWindow()
    {
        InitializeComponent();
        AddLog("Application started");
    }

    private void WorkspaceList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WorkspaceList.SelectedItem is not ListBoxItem selectedItem)
            return;

        var workspaceName = selectedItem.Content?.ToString() ?? "Unknown workspace";

        WorkspaceTitle.Text = workspaceName;
        ResumeNoteText.Text = GetFakeResumeNote(workspaceName);

        AddLog($"Workspace selected: {workspaceName}");
    }

    // TODO: убрать хардкод
    private string GetFakeResumeNote(string workspaceName) => workspaceName switch
    {
        "Action Inbox" =>
            "Остановился на OpenTelemetry. Следующий шаг — добавить trace для GenerateActionsJob и проверить span в Aspire Dashboard.",

        "Изучение Redis" =>
            "Следующий шаг — повторить структуры данных Redis и написать маленький пример с cache invalidation.",

        "Статья про OpenTelemetry" =>
            "Нужно дописать раздел про traces и показать пример цепочки StartWorkspace → RunChecks → ExecuteActions.",

        "Home Server" =>
            "Проверить docker compose файл и список сервисов, которые должны стартовать после reboot.",

        "Weekly Review" =>
            "Собрать parking lot заметки и выбрать 3 главных задачи на следующую неделю.",

        _ =>
            "Пока нет resume note."
    };

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;
        
        var actionName = button.Tag?.ToString() ?? button.Content?.ToString() ?? "Unknown action";
        AddLog($"Action clicked: {actionName}");
    }

    private void NewWorkspace_Click(object sender, RoutedEventArgs e)
    {
        AddLog("New Workspace created");
        MessageBox.Show("Позже здесь будет создание workspace.", "Workspace Cockpit");
    }
}