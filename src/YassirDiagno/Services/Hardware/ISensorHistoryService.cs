using YassirDiagno.Models;

namespace YassirDiagno.Services.Hardware;

public sealed class SensorHistory
{
    private readonly Queue<(DateTime time, double value)> _data = new();
    private readonly int _maxPoints;

    public SensorHistory(int maxPoints) => _maxPoints = maxPoints;

    public void Add(DateTime time, double value)
    {
        _data.Enqueue((time, value));
        while (_data.Count > _maxPoints) _data.Dequeue();
    }

    public IReadOnlyList<(DateTime time, double value)> Snapshot() => _data.ToArray();
    public int Count => _data.Count;
    public double Min => _data.Count == 0 ? 0 : _data.Min(x => x.value);
    public double Max => _data.Count == 0 ? 0 : _data.Max(x => x.value);
    public double Avg => _data.Count == 0 ? 0 : _data.Average(x => x.value);
    public double Latest => _data.Count == 0 ? 0 : _data.Last().value;
}

public interface ISensorHistoryService : IDisposable
{
    SensorHistory Cpu { get; }
    SensorHistory Gpu { get; }
    SensorHistory Ssd { get; }
    SensorHistory Battery { get; }
    SensorHistory Power { get; }
    SensorHistory Health { get; }
    SensorHistory Chassis { get; }
    event EventHandler? HistoryUpdated;
    HardwareSnapshot? LastSnapshot { get; }
}

public sealed class SensorHistoryService : ISensorHistoryService
{
    private readonly IHardwareMonitorService _monitor;
    private const int MaxPoints = 600;

    public SensorHistory Cpu { get; } = new(MaxPoints);
    public SensorHistory Gpu { get; } = new(MaxPoints);
    public SensorHistory Ssd { get; } = new(MaxPoints);
    public SensorHistory Battery { get; } = new(MaxPoints);
    public SensorHistory Power { get; } = new(MaxPoints);
    public SensorHistory Health { get; } = new(MaxPoints);
    public SensorHistory Chassis { get; } = new(MaxPoints);

    public HardwareSnapshot? LastSnapshot { get; private set; }

    public event EventHandler? HistoryUpdated;

    public SensorHistoryService(IHardwareMonitorService monitor)
    {
        _monitor = monitor;
        _monitor.SnapshotUpdated += OnSnapshot;
    }

    private void OnSnapshot(object? sender, HardwareSnapshot s)
    {
        LastSnapshot = s;
        var t = s.Timestamp;
        if (s.CpuSilicon is double a) Cpu.Add(t, a);
        if (s.GpuZone is double b)     Gpu.Add(t, b);
        if (s.Ssd is double c)          Ssd.Add(t, c);
        if (s.BatteryLevel is double d) Battery.Add(t, d);
        if (s.CpuPower is double e)     Power.Add(t, e);
        if (s.ExtZone is double f)      Chassis.Add(t, f);
        Health.Add(t, s.GlobalHealthScore);
        HistoryUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _monitor.SnapshotUpdated -= OnSnapshot;
    }
}
