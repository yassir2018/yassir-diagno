using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using YassirDiagno.Models;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IHardwareMonitorService _monitor;

    private const int MaxPoints = 60;
    private const double SparklineWidth = 240;
    private const double SparklineHeight = 32;

    private readonly Queue<double> _cpuHist = new();
    private readonly Queue<double> _gpuHist = new();
    private readonly Queue<double> _ssdHist = new();
    private readonly Queue<double> _batHist = new();
    private readonly Queue<double> _pwrHist = new();
    private readonly Queue<double> _healthHist = new();

    [ObservableProperty] private HardwareSnapshot _snapshot = HardwareSnapshot.Empty();
    [ObservableProperty] private HardwareIdentity _identity = new("CPU", "GPU", "SSD", "Battery", "System");
    [ObservableProperty] private string _lastUpdate = "initialisation...";

    [ObservableProperty] private PointCollection _cpuSparkline = new();
    [ObservableProperty] private PointCollection _gpuSparkline = new();
    [ObservableProperty] private PointCollection _ssdSparkline = new();
    [ObservableProperty] private PointCollection _batSparkline = new();
    [ObservableProperty] private PointCollection _pwrSparkline = new();
    [ObservableProperty] private PointCollection _healthSparkline = new();

    public DashboardViewModel(IHardwareMonitorService monitor)
    {
        _monitor = monitor;
    }

    public override Task LoadAsync(CancellationToken ct = default)
    {
        Identity = _monitor.GetIdentity();
        _monitor.SnapshotUpdated += OnSnapshotUpdated;
        OnSnapshotUpdated(this, _monitor.ReadSnapshot());
        return Task.CompletedTask;
    }

    public override Task UnloadAsync(CancellationToken ct = default)
    {
        _monitor.SnapshotUpdated -= OnSnapshotUpdated;
        return Task.CompletedTask;
    }

    private void OnSnapshotUpdated(object? sender, HardwareSnapshot snap)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            Snapshot = snap;
            LastUpdate = snap.Timestamp.ToString("HH:mm:ss");

            if (snap.CpuSilicon is double cpu)    AddPoint(_cpuHist, cpu);
            if (snap.GpuZone is double gpu)        AddPoint(_gpuHist, gpu);
            if (snap.Ssd is double ssd)            AddPoint(_ssdHist, ssd);
            if (snap.BatteryLevel is double bat)   AddPoint(_batHist, bat);
            if (snap.CpuPower is double pwr)       AddPoint(_pwrHist, pwr);
            AddPoint(_healthHist, snap.GlobalHealthScore);

            CpuSparkline    = BuildSparkline(_cpuHist);
            GpuSparkline    = BuildSparkline(_gpuHist);
            SsdSparkline    = BuildSparkline(_ssdHist);
            BatSparkline    = BuildSparkline(_batHist);
            PwrSparkline    = BuildSparkline(_pwrHist);
            HealthSparkline = BuildSparkline(_healthHist);
        });
    }

    private static void AddPoint(Queue<double> q, double v)
    {
        q.Enqueue(v);
        while (q.Count > MaxPoints) q.Dequeue();
    }

    private static PointCollection BuildSparkline(IEnumerable<double> values)
    {
        var arr = values.ToArray();
        if (arr.Length < 2) return new PointCollection();

        var min = arr.Min();
        var max = arr.Max();
        var range = Math.Max(max - min, 1);
        var pad = 4.0;
        var usableH = SparklineHeight - pad * 2;

        var points = new PointCollection();
        for (int i = 0; i < arr.Length; i++)
        {
            var x = i / (double)(arr.Length - 1) * SparklineWidth;
            var y = pad + (usableH - (arr[i] - min) / range * usableH);
            points.Add(new Point(x, y));
        }
        return points;
    }
}
