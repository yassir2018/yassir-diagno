using System.Timers;
using YassirDiagno.Models;
using Timer = System.Timers.Timer;

namespace YassirDiagno.Services.Hardware;

public sealed class StubHardwareMonitorService : IHardwareMonitorService
{
    private Timer? _timer;
    private readonly Random _rng = new();

    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

    public HardwareIdentity GetIdentity() => new(
        CpuName: "AMD Ryzen 5 PRO 7530U",
        GpuName: "AMD Radeon Graphics",
        SsdName: "KIOXIA KBG50ZNV256G",
        BatteryName: "HP RH03051XL",
        MotherboardName: "HP EliteBook 645 G10");

    public HardwareSnapshot ReadSnapshot()
    {
        var cpu = 45 + _rng.NextDouble() * 25;
        var gpu = 35 + _rng.NextDouble() * 10;
        var ssd = 30 + _rng.NextDouble() * 5;
        var battery = 70 + _rng.NextDouble() * 25;
        var batHealth = 70 + _rng.NextDouble() * 5;
        var power = 3 + _rng.NextDouble() * 10;
        var gpuLoad = _rng.NextDouble() * 25;
        var chassis = 35 + _rng.NextDouble() * 8;
        var pkg = cpu - 5 + _rng.NextDouble() * 3;

        var score = 100;
        if (cpu > 75) score -= 20;
        var label = score >= 85 ? "BON" : score >= 70 ? "OK" : score >= 50 ? "ATTENTION" : "MAUVAIS";

        return new HardwareSnapshot(
            DateTime.Now, cpu, power, pkg, gpu, gpuLoad, ssd, chassis,
            battery, batHealth, "discharging", score, label);
    }

    public event EventHandler<HardwareSnapshot>? SnapshotUpdated;

    public void StartPolling(TimeSpan interval)
    {
        _timer?.Dispose();
        _timer = new Timer(interval.TotalMilliseconds);
        _timer.Elapsed += (_, _) => SnapshotUpdated?.Invoke(this, ReadSnapshot());
        _timer.AutoReset = true;
        _timer.Start();
    }

    public void StopPolling()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose() => StopPolling();
}
