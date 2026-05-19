using System.Management;
using System.Timers;
using LibreHardwareMonitor.Hardware;
using YassirDiagno.Models;
using LhmSensorType = LibreHardwareMonitor.Hardware.SensorType;
using Timer = System.Timers.Timer;

namespace YassirDiagno.Services.Hardware;

public sealed class LhmHardwareMonitorService : IHardwareMonitorService
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsStorageEnabled = true,
        IsBatteryEnabled = true,
        IsMotherboardEnabled = true,
    };

    private Timer? _timer;
    private HardwareIdentity? _identity;
    private bool _opened;

    public event EventHandler<HardwareSnapshot>? SnapshotUpdated;

    public Task InitializeAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            if (_opened) return;
            _computer.Open();
            _opened = true;
            _identity = ResolveIdentity();
        }, ct);
    }

    public HardwareIdentity GetIdentity() => _identity ?? new HardwareIdentity("CPU", "GPU", "SSD", "Battery", "System");

    public HardwareSnapshot ReadSnapshot()
    {
        if (!_opened) return HardwareSnapshot.Empty();

        double? cpuSilicon = null, cpuPower = null, ssd = null,
                gpuLoad = null, battLevel = null, battHealth = null;
        string? battStatus = null;

        foreach (var hw in _computer.Hardware)
        {
            hw.Update();
            foreach (var sub in hw.SubHardware) sub.Update();

            switch (hw.HardwareType)
            {
                case HardwareType.Cpu:
                    foreach (var s in hw.Sensors)
                    {
                        if (s.Value is null) continue;
                        if (s.SensorType == LhmSensorType.Temperature &&
                            (s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                             s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
                             s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase)))
                        {
                            var v = s.Value.Value;
                            if (v > 0 && v < 150 && (cpuSilicon is null || v > cpuSilicon))
                                cpuSilicon = v;
                        }
                        else if (s.SensorType == LhmSensorType.Power && s.Name == "Package")
                        {
                            cpuPower = s.Value;
                        }
                    }
                    break;

                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                case HardwareType.GpuNvidia:
                    foreach (var s in hw.Sensors)
                    {
                        if (s.Value is null) continue;
                        if (s.SensorType == LhmSensorType.Load &&
                            (s.Name.Contains("D3D 3D") || s.Name.Contains("GPU Core")))
                        {
                            if (gpuLoad is null || s.Value > gpuLoad) gpuLoad = s.Value;
                        }
                    }
                    break;

                case HardwareType.Storage:
                    foreach (var s in hw.Sensors)
                    {
                        if (s.Value is null) continue;
                        if (s.SensorType == LhmSensorType.Temperature && s.Name.Contains("Composite"))
                            ssd = s.Value;
                    }
                    break;

                case HardwareType.Battery:
                    foreach (var s in hw.Sensors)
                    {
                        if (s.Value is null) continue;
                        if (s.Name == "Charge Level") battLevel = s.Value;
                        else if (s.Name == "Degradation Level") battHealth = 100 - s.Value;
                    }
                    break;
            }
        }

        double? cpuZone = null, gpuZone = null, extZone = null;
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM MSAcpi_ThermalZoneTemperature");
            foreach (var obj in searcher.Get())
            {
                var name = obj["InstanceName"]?.ToString() ?? "";
                var raw = Convert.ToDouble(obj["CurrentTemperature"]);
                var c = raw / 10.0 - 273.15;
                if (c <= 0 || c >= 150) continue;
                if (name.Contains("CPUZ")) cpuZone = c;
                else if (name.Contains("GFXZ")) gpuZone = c;
                else if (name.Contains("EXTZ")) extZone = c;
            }
        }
        catch { /* WMI not accessible without admin */ }

        battStatus = ResolveBatteryStatus();

        var score = ComputeHealthScore(cpuSilicon, ssd, battHealth, cpuSilicon - extZone);
        var label = score >= 85 ? "BON" : score >= 70 ? "OK" : score >= 50 ? "ATTENTION" : "MAUVAIS";

        return new HardwareSnapshot(
            DateTime.Now, cpuSilicon, cpuPower, cpuZone, gpuZone, gpuLoad, ssd, extZone,
            battLevel, battHealth, battStatus, score, label);
    }

    private static int ComputeHealthScore(double? cpu, double? ssd, double? batHealth, double? thermalDelta)
    {
        var score = 100;
        if (cpu is double c)
        {
            if (c >= 85) score -= 35;
            else if (c >= 75) score -= 20;
            else if (c >= 65) score -= 8;
        }
        if (ssd is double s)
        {
            if (s >= 78) score -= 25;
            else if (s >= 65) score -= 12;
            else if (s >= 50) score -= 4;
        }
        if (batHealth is double bh)
        {
            if (bh < 50) score -= 20;
            else if (bh < 70) score -= 10;
            else if (bh < 85) score -= 4;
        }
        if (thermalDelta is double d)
        {
            if (d > 35) score -= 15;
            else if (d > 25) score -= 6;
        }
        return Math.Max(0, score);
    }

    private static string? ResolveBatteryStatus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT BatteryStatus FROM Win32_Battery");
            foreach (var obj in searcher.Get())
            {
                var status = Convert.ToInt32(obj["BatteryStatus"]);
                return status switch
                {
                    1 => "discharging",
                    2 => "on AC",
                    3 => "full",
                    4 => "LOW",
                    5 => "CRITICAL",
                    6 or 7 => "charging",
                    8 => "charging (low)",
                    9 => "charging (crit)",
                    11 => "partial",
                    _ => "unknown"
                };
            }
        }
        catch { }
        return null;
    }

    private HardwareIdentity ResolveIdentity()
    {
        string cpu = "CPU", gpu = "GPU", ssd = "SSD", bat = "Battery", mb = "System";

        foreach (var hw in _computer.Hardware)
        {
            switch (hw.HardwareType)
            {
                case HardwareType.Cpu:
                    cpu = ShortenName(hw.Name);
                    break;
                case HardwareType.GpuAmd:
                case HardwareType.GpuIntel:
                case HardwareType.GpuNvidia:
                    gpu = ShortenName(hw.Name);
                    break;
                case HardwareType.Storage:
                    ssd = ShortenName(hw.Name);
                    break;
                case HardwareType.Battery:
                    bat = ShortenName(hw.Name);
                    break;
            }
        }

        try
        {
            using var s = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (var obj in s.Get())
            {
                var manu = obj["Manufacturer"]?.ToString() ?? "";
                var model = obj["Model"]?.ToString() ?? "";
                var combined = model.StartsWith(manu, StringComparison.OrdinalIgnoreCase) ? model : $"{manu} {model}";
                combined = System.Text.RegularExpressions.Regex.Replace(combined, @"\s+Notebook PC$", "");
                combined = System.Text.RegularExpressions.Regex.Replace(combined, @"\s+\d+ inch", "");
                mb = ShortenName(combined);
            }
        }
        catch { }

        try
        {
            using var s = new ManagementObjectSearcher("SELECT Name FROM Win32_PortableBattery");
            foreach (var obj in s.Get())
            {
                var name = obj["Name"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name)) bat = ShortenName(name);
            }
        }
        catch { }

        return new HardwareIdentity(cpu, gpu, ssd, bat, mb);
    }

    private static string ShortenName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "unknown";
        var n = name;
        n = System.Text.RegularExpressions.Regex.Replace(n, @"AMD\s+", "");
        n = System.Text.RegularExpressions.Regex.Replace(n, @"Intel\(R\)\s+", "");
        n = System.Text.RegularExpressions.Regex.Replace(n, @"NVIDIA\s+", "");
        n = System.Text.RegularExpressions.Regex.Replace(n, @"\(TM\)|\(R\)", "");
        n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+with Radeon Graphics", "");
        n = System.Text.RegularExpressions.Regex.Replace(n, @"\s+", " ").Trim();
        if (n.Length > 28) n = n[..28].TrimEnd() + "…";
        return n;
    }

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

    public void Dispose()
    {
        StopPolling();
        if (_opened) _computer.Close();
    }
}
