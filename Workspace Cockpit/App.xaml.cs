using System.Windows;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Workspace_Cockpit.Helpers;
using Workspace_Cockpit.Windows;

namespace Workspace_Cockpit;

public partial class App : Application
{
    private ServiceProvider? serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        serviceProvider = services.BuildServiceProvider();

        try
        {
            await serviceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(null, "Database error", ex.Message);
        }

        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        serviceProvider?.Dispose();
        base.OnExit(e);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddWorkspaceCockpitInfrastructure();
        services.AddSingleton<MainWindow>();
    }
}