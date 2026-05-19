using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YassirDiagno.Services;

namespace YassirDiagno.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IThemeService _theme;
    private readonly IAutoStartService _autoStart;

    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private int _pollingIntervalSeconds;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _showSparklines;
    [ObservableProperty] private bool _notificationsEnabled;
    [ObservableProperty] private string _saveStatus = "";

    public SettingsPageViewModel(ISettingsService settings, IThemeService theme, IAutoStartService autoStart)
    {
        _settings = settings;
        _theme = theme;
        _autoStart = autoStart;
        IsDarkTheme = _theme.Current == AppTheme.Dark;
        PollingIntervalSeconds = _settings.Current.PollingIntervalSeconds;
        StartWithWindows = _autoStart.IsEnabled();
        StartMinimized = _settings.Current.StartMinimized;
        ShowSparklines = _settings.Current.ShowSparklines;
        NotificationsEnabled = _settings.Current.NotificationsEnabled;
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
}
