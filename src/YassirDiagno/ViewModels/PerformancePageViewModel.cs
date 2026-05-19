using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.ViewModels;

public partial class PerformancePageViewModel : ViewModelBase
{
    private readonly ISensorHistoryService _history;

    private const double ChartWidth = 720;
    private const double ChartHeight = 140;

    [ObservableProperty] private PointCollection _cpuChart = new();
    [ObservableProperty] private PointCollection _gpuChart = new();
    [ObservableProperty] private PointCollection _ssdChart = new();
    [ObservableProperty] private PointCollection _powerChart = new();
    [ObservableProperty] private PointCollection _batteryChart = new();

    [ObservableProperty] private string _cpuStats = "--";
    [ObservableProperty] private string _gpuStats = "--";
    [ObservableProperty] private string _ssdStats = "--";
    [ObservableProperty] private string _powerStats = "--";
    [ObservableProperty] private string _batteryStats = "--";

    [ObservableProperty] private int _historySeconds;
    [ObservableProperty] private string _windowLabel = "Live (60s)";
    [ObservableProperty] private int _selectedWindowSeconds = 60;

    public int[] WindowOptions { get; } = new[] { 60, 300, 600 };

    public PerformancePageViewModel(ISensorHistoryService history)
    {
        _history = history;
    }

    public override Task LoadAsync(CancellationToken ct = default)
    {
        _history.HistoryUpdated += OnHistoryUpdated;
        Refresh();
        return Task.CompletedTask;
    }

    public override Task UnloadAsync(CancellationToken ct = default)
    {
        _history.HistoryUpdated -= OnHistoryUpdated;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void SelectWindow(string seconds)
    {
        if (int.TryParse(seconds, out var s))
        {
            SelectedWindowSeconds = s;
            WindowLabel = s switch
            {
                60 => "Live (60s)",
                300 => "5 minutes",
                600 => "10 minutes",
                _ => $"{s}s"
            };
            Refresh();
        }
    }

    private void OnHistoryUpdated(object? sender, EventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(Refresh);
    }

    private void Refresh()
    {
        CpuChart     = BuildChart(_history.Cpu, SelectedWindowSeconds);
        GpuChart     = BuildChart(_history.Gpu, SelectedWindowSeconds);
        SsdChart     = BuildChart(_history.Ssd, SelectedWindowSeconds);
        PowerChart   = BuildChart(_history.Power, SelectedWindowSeconds);
        BatteryChart = BuildChart(_history.Battery, SelectedWindowSeconds);

        CpuStats     = FormatStats(_history.Cpu, "°C");
        GpuStats     = FormatStats(_history.Gpu, "°C");
        SsdStats     = FormatStats(_history.Ssd, "°C");
        PowerStats   = FormatStats(_history.Power, "W");
        BatteryStats = FormatStats(_history.Battery, "%");

        HistorySeconds = _history.Cpu.Count;
    }

    private static PointCollection BuildChart(SensorHistory h, int windowSeconds)
    {
        var snap = h.Snapshot();
        if (snap.Count < 2) return new PointCollection();

        var cutoff = DateTime.Now.AddSeconds(-windowSeconds);
        var filtered = snap.Where(p => p.time >= cutoff).ToArray();
        if (filtered.Length < 2) filtered = snap.ToArray();

        var values = filtered.Select(p => p.value).ToArray();
        var min = values.Min();
        var max = values.Max();
        var range = Math.Max(max - min, 1);
        var pad = 8.0;
        var usableH = ChartHeight - pad * 2;

        var points = new PointCollection(filtered.Length);
        for (int i = 0; i < filtered.Length; i++)
        {
            var x = i / (double)(filtered.Length - 1) * ChartWidth;
            var y = pad + (usableH - (values[i] - min) / range * usableH);
            points.Add(new Point(x, y));
        }
        return points;
    }

    private static string FormatStats(SensorHistory h, string unit)
    {
        if (h.Count == 0) return "--";
        return $"actuel {h.Latest:F1}{unit}  ·  min {h.Min:F1}{unit}  ·  max {h.Max:F1}{unit}  ·  moy {h.Avg:F1}{unit}";
    }
}
