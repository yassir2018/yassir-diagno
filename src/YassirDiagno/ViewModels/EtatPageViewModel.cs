using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using YassirDiagno.Models;
using YassirDiagno.Services;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.ViewModels;

public partial class EtatPageViewModel : ViewModelBase
{
    private readonly IHardwareMonitorService _monitor;
    private readonly IComponentHealthService _health;

    [ObservableProperty] private ObservableCollection<ComponentHealth> _components = new();
    [ObservableProperty] private string _lastUpdate = "--";

    public EtatPageViewModel(IHardwareMonitorService monitor, IComponentHealthService health)
    {
        _monitor = monitor;
        _health = health;
    }

    public override Task LoadAsync(CancellationToken ct = default)
    {
        _monitor.SnapshotUpdated += OnSnapshot;
        OnSnapshot(this, _monitor.ReadSnapshot());
        return Task.CompletedTask;
    }

    public override Task UnloadAsync(CancellationToken ct = default)
    {
        _monitor.SnapshotUpdated -= OnSnapshot;
        return Task.CompletedTask;
    }

    private void OnSnapshot(object? sender, HardwareSnapshot s)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var list = _health.Analyze(s, _monitor.GetIdentity());
            Components.Clear();
            foreach (var c in list) Components.Add(c);
            LastUpdate = s.Timestamp.ToString("HH:mm:ss");
        });
    }
}
