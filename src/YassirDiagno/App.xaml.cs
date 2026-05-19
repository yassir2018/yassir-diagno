using System.IO;
using System.Windows;
using H.NotifyIcon;
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
    private TaskbarIcon? _trayIcon;

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

            splash.SetProgress(30, "Application du thème...");
            _ = Host.Services.GetRequiredService<IThemeService>();

            splash.SetProgress(45, "Initialisation du driver hardware...");
            var monitor = Host.Services.GetRequiredService<IHardwareMonitorService>();
            await monitor.InitializeAsync();

            splash.SetProgress(60, "Démarrage du collecteur d'historique...");
            _ = Host.Services.GetRequiredService<ISensorHistoryService>();
            monitor.StartPolling(TimeSpan.FromSeconds(1));

            splash.SetProgress(75, "Démarrage du service de notifications...");
            var notif = Host.Services.GetRequiredService<INotificationService>();
            notif.Start();

            splash.SetProgress(85, "Préparation du tableau de bord...");
            var main = Host.Services.GetRequiredService<MainWindow>();

            splash.SetProgress(95, "Configuration de la barre d'état...");
            SetupTrayIcon(main);

            splash.SetProgress(100, "Prêt");
            await Task.Delay(200);

            var settings = Host.Services.GetRequiredService<ISettingsService>();
            if (!settings.Current.StartMinimized) main.Show();
            splash.Close();

            WriteCrash("Startup", null, "MainWindow ready");

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            WriteCrash("OnStartup", ex);
            MessageBox.Show($"Erreur startup: {ex.Message}\n\nDetails dans: {LogPath}", "Yassir Diagno", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void SetupTrayIcon(MainWindow main)
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Yassir Diagno — Thermal Monitor",
            Visibility = Visibility.Visible,
            NoLeftClickDelay = true
        };

        try
        {
            var iconUri = new Uri("pack://application:,,,/Resources/Icons/yassir.ico", UriKind.Absolute);
            _trayIcon.IconSource = new System.Windows.Media.Imaging.BitmapImage(iconUri);
        }
        catch { }

        var menu = new System.Windows.Controls.ContextMenu();

        var showItem = new System.Windows.Controls.MenuItem { Header = "Afficher Yassir Diagno" };
        showItem.Click += (_, _) =>
        {
            main.Show();
            if (main.WindowState == WindowState.Minimized) main.WindowState = WindowState.Normal;
            main.Activate();
        };
        menu.Items.Add(showItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Quitter" };
        exitItem.Click += (_, _) =>
        {
            main.ForceClose = true;
            Shutdown();
        };
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenu = menu;
        _trayIcon.TrayLeftMouseDown += (_, _) =>
        {
            if (main.IsVisible) main.Hide();
            else { main.Show(); main.Activate(); }
        };
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IHardwareMonitorService, LhmHardwareMonitorService>();
        services.AddSingleton<ISystemInfoService, SystemInfoService>();
        services.AddSingleton<IHardwareInventoryService, HardwareInventoryService>();
        services.AddSingleton<ISensorHistoryService, SensorHistoryService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IAutoStartService, AutoStartService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ICsvLoggingService, CsvLoggingService>();
        services.AddSingleton<IDiagnosticService, DiagnosticService>();
        services.AddSingleton<IStressTestService, StressTestService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IProcessExplorerService, ProcessExplorerService>();
        services.AddSingleton<IComponentHealthService, ComponentHealthService>();
        services.AddSingleton<IUpdateCheckService, UpdateCheckService>();
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SystemPageViewModel>();
        services.AddSingleton<HardwarePageViewModel>();
        services.AddSingleton<PerformancePageViewModel>();
        services.AddSingleton<ProcessesPageViewModel>();
        services.AddSingleton<EtatPageViewModel>();
        services.AddSingleton<ToolsPageViewModel>();
        services.AddSingleton<ReportsPageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();

        services.AddSingleton<MainWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
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
