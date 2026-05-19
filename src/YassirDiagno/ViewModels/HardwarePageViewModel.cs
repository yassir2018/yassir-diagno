using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using YassirDiagno.Models;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.ViewModels;

public partial class HardwarePageViewModel : ViewModelBase
{
    private readonly IHardwareInventoryService _inv;
    private readonly IHardwareMonitorService _monitor;

    [ObservableProperty] private ObservableCollection<HardwareItem> _items = new();
    [ObservableProperty] private ObservableCollection<CpuCoreInfo> _cores = new();
    [ObservableProperty] private double _cpuTotalLoad;
    [ObservableProperty] private double _cpuAverageClockGhz;
    [ObservableProperty] private string _cpuName = "";
    [ObservableProperty] private bool _isLoading = true;

    public HardwarePageViewModel(IHardwareInventoryService inv, IHardwareMonitorService monitor)
    {
        _inv = inv;
        _monitor = monitor;
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        Items = await _inv.GetInventoryAsync();
        CpuName = _monitor.GetIdentity().CpuName;
        _monitor.SnapshotUpdated += OnSnapshot;
        OnSnapshot(this, _monitor.ReadSnapshot());
        IsLoading = false;
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
            CpuTotalLoad = s.CpuTotalLoad ?? 0;
            CpuAverageClockGhz = (s.CpuAverageClock ?? 0) / 1000.0;
            if (s.Cores is { Count: > 0 })
            {
                Cores.Clear();
                foreach (var c in s.Cores) Cores.Add(c);
            }
        });
    }
}
