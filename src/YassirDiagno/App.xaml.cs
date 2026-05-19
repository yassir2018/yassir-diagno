using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YassirDiagno.Services;
using YassirDiagno.Services.Hardware;
using YassirDiagno.ViewModels;

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

            Host = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            WriteCrash("Startup", null, "Host built");

            await Host.StartAsync();
            WriteCrash("Startup", null, "Host started");

            var main = Host.Services.GetRequiredService<MainWindow>();
            WriteCrash("Startup", null, "MainWindow resolved");

            main.Show();
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
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();

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
