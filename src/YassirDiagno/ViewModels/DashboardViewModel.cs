using CommunityToolkit.Mvvm.ComponentModel;
using YassirDiagno.Models;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IHardwareMonitorService _monitor;

    [ObservableProperty] private HardwareSnapshot _snapshot = HardwareSnapshot.Empty();
    [ObservableProperty] private HardwareIdentity _identity = new("CPU", "GPU", "SSD", "Battery", "System");
    [ObservableProperty] private string _lastUpdate = "initialisation...";

    public DashboardViewModel(IHardwareMonitorService monitor)
    {
        _monitor = monitor;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        await _monitor.InitializeAsync(ct);
        Identity = _monitor.GetIdentity();
        _monitor.SnapshotUpdated += OnSnapshotUpdated;
        _monitor.StartPolling(TimeSpan.FromSeconds(1));
    }

    public override Task UnloadAsync(CancellationToken ct = default)
    {
        _monitor.SnapshotUpdated -= OnSnapshotUpdated;
        _monitor.StopPolling();
        return Task.CompletedTask;
    }

    private void OnSnapshotUpdated(object? sender, HardwareSnapshot snap)
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            Snapshot = snap;
            LastUpdate = snap.Timestamp.ToString("HH:mm:ss");
        });
    }
}
