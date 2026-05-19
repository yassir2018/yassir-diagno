using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YassirDiagno.Services;
using YassirDiagno.Services.Hardware;
using YassirDiagno.ViewModels;
using YassirDiagno.Views;

namespace YassirDiagno;

public partial class App : Application
{
    public static IHost? Host { get; private set; }

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YassirDiagno", "crash.log");

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            WriteCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        DispatcherUnhandledException += (_, e) =>
        {
            WriteCrash("Dispatcher.UnhandledException", e.Exception);
            e.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrash("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        try
        {
            WriteCrash("Startup", null, "OnStartup begin");

            var splash = new SplashWindow();
            splash.Show();
            splash.SetProgress(5, "Initialisation des services...");
            await Task.Delay(50);

            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            splash.SetProgress(20, "Démarrage du hôte d'application...");
            await Host.StartAsync();

            splash.SetProgress(35, "Initialisation du driver hardware...");
            var monitor = Host.Services.GetRequiredService<IHardwareMonitorService>();
            await monitor.InitializeAsync();

            splash.SetProgress(55, "Démarrage du collecteur d'historique...");
            _ = Host.Services.GetRequiredService<ISensorHistoryService>();
            monitor.StartPolling(TimeSpan.FromSeconds(1));

            splash.SetProgress(75, "Détection des composants...");
            await Task.Delay(150);

            splash.SetProgress(90, "Préparation du tableau de bord...");
            var main = Host.Services.GetRequiredService<MainWindow>();
            await Task.Delay(150);

            splash.SetProgress(100, "Prêt");
            await Task.Delay(200);

            main.Show();
            splash.Close();

            WriteCrash("Startup", null, "MainWindow shown");

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            WriteCrash("OnStartup", ex);
            MessageBox.Show($"Erreur startup: {ex.Message}\n\nDetails dans: {LogPath}", "Yassir Diagno", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHardwareMonitorService, LhmHardwareMonitorService>();
        services.AddSingleton<ISystemInfoService, SystemInfoService>();
        services.AddSingleton<IHardwareInventoryService, HardwareInventoryService>();
        services.AddSingleton<ISensorHistoryService, SensorHistoryService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDiagnosticService, DiagnosticService>();
        services.AddSingleton<IStressTestService, StressTestService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SystemPageViewModel>();
        services.AddSingleton<HardwarePageViewModel>();
        services.AddSingleton<PerformancePageViewModel>();
        services.AddSingleton<ToolsPageViewModel>();
        services.AddSingleton<ReportsPageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();

        services.AddSingleton<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (Host is not null)
        {
            await Host.StopAsync();
            Host.Dispose();
        }
        base.OnExit(e);
    }

    private static void WriteCrash(string source, Exception? ex, string? note = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            var msg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}] {note ?? ex?.ToString() ?? "(no detail)"}";
            File.AppendAllText(LogPath, msg + Environment.NewLine);
        }
        catch { }
    }
}
