using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YassirDiagno.Services;

namespace YassirDiagno.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IAutoStartService _autoStart;
    private readonly ICsvLoggingService _logging;

    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private int _pollingIntervalSeconds;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _showSparklines;
    [ObservableProperty] private bool _notificationsEnabled;
    [ObservableProperty] private string _saveStatus = "";

    [ObservableProperty] private bool _isLogging;
    [ObservableProperty] private string _logPath = "";
    [ObservableProperty] private int _logRows;
    [ObservableProperty] private string _logSizeFormatted = "0 KB";

    public SettingsPageViewModel(ISettingsService settings, IThemeService theme, IAutoStartService autoStart, ICsvLoggingService logging)
    {
        _settings = settings;
        _theme = theme;
        _autoStart = autoStart;
        _logging = logging;
        _logging.StatusChanged += (_, _) => RefreshLogging();

        IsDarkTheme = _theme.Current == AppTheme.Dark;
        PollingIntervalSeconds = _settings.Current.PollingIntervalSeconds;
        StartWithWindows = _autoStart.IsEnabled();
        StartMinimized = _settings.Current.StartMinimized;
        ShowSparklines = _settings.Current.ShowSparklines;
        NotificationsEnabled = _settings.Current.NotificationsEnabled;
        RefreshLogging();
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _theme.Apply(value ? AppTheme.Dark : AppTheme.Light);
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Current.PollingIntervalSeconds = Math.Max(1, Math.Min(60, PollingIntervalSeconds));
        _settings.Current.StartMinimized = StartMinimized;
        _settings.Current.ShowSparklines = ShowSparklines;
        _settings.Current.StartWithWindows = StartWithWindows;
        _settings.Current.NotificationsEnabled = NotificationsEnabled;
        _settings.Current.Theme = IsDarkTheme ? "Dark" : "Light";
        _settings.Save();
        _autoStart.SetEnabled(StartWithWindows);
        SaveStatus = $"✓ Sauvegardé à {DateTime.Now:HH:mm:ss}";
    }

    [RelayCommand]
    private void Reset()
    {
        PollingIntervalSeconds = 1;
        StartWithWindows = false;
        StartMinimized = false;
        ShowSparklines = true;
        NotificationsEnabled = true;
        IsDarkTheme = false;
        Save();
    }

    [RelayCommand]
    private void ToggleLogging()
    {
        if (_logging.IsLogging) _logging.Stop();
        else _logging.Start();
        RefreshLogging();
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YassirDiagno", "logs");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        try { Process.Start("explorer.exe", $"\"{dir}\""); } catch { }
    }

    private void RefreshLogging()
    {
        IsLogging = _logging.IsLogging;
        LogPath = _logging.CurrentLogPath;
        LogRows = _logging.RowsWritten;
        LogSizeFormatted = FormatSize(_logging.CurrentLogSize);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / 1024.0 / 1024.0:F2} MB";
    }
}
