using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YassirDiagno.Services;

namespace YassirDiagno.ViewModels;

public partial class SettingsPageViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty] private int _pollingIntervalSeconds;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _showSparklines;
    [ObservableProperty] private string _saveStatus = "";

    public SettingsPageViewModel(ISettingsService settings)
    {
        _settings = settings;
        PollingIntervalSeconds = _settings.Current.PollingIntervalSeconds;
        StartWithWindows = _settings.Current.StartWithWindows;
        StartMinimized = _settings.Current.StartMinimized;
        ShowSparklines = _settings.Current.ShowSparklines;
    }

    [RelayCommand]
    private void Save()
    {
        _settings.Current.PollingIntervalSeconds = Math.Max(1, Math.Min(60, PollingIntervalSeconds));
        _settings.Current.StartWithWindows = StartWithWindows;
        _settings.Current.StartMinimized = StartMinimized;
        _settings.Current.ShowSparklines = ShowSparklines;
        _settings.Save();
        SaveStatus = $"✓ Sauvegardé à {DateTime.Now:HH:mm:ss}";
    }

    [RelayCommand]
    private void Reset()
    {
        PollingIntervalSeconds = 1;
        StartWithWindows = false;
        StartMinimized = false;
        ShowSparklines = true;
        Save();
    }
}
