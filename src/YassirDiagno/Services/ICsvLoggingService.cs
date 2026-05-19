using System.Globalization;
using System.IO;
using YassirDiagno.Models;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.Services;

public interface ICsvLoggingService : IDisposable
{
    bool IsLogging { get; }
    string CurrentLogPath { get; }
    long CurrentLogSize { get; }
    int RowsWritten { get; }
    event EventHandler? StatusChanged;
    void Start();
    void Stop();
}

public sealed class CsvLoggingService : ICsvLoggingService
{
    private readonly IHardwareMonitorService _monitor;
    private readonly ISettingsService _settings;
    private StreamWriter? _writer;
    private readonly object _lock = new();

    public bool IsLogging { get; private set; }
    public string CurrentLogPath { get; private set; } = "";
    public long CurrentLogSize { get; private set; }
    public int RowsWritten { get; private set; }
    public event EventHandler? StatusChanged;

    public CsvLoggingService(IHardwareMonitorService monitor, ISettingsService settings)
    {
        _monitor = monitor;
        _settings = settings;
    }

    public void Start()
    {
        if (IsLogging) return;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YassirDiagno", "logs");
        Directory.CreateDirectory(dir);

        CurrentLogPath = Path.Combine(dir, $"sensors-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        _writer = new StreamWriter(CurrentLogPath, append: false, System.Text.Encoding.UTF8) { AutoFlush = true };
        _writer.WriteLine("Timestamp;CpuSilicon_C;CpuPower_W;CpuZone_C;GpuZone_C;GpuLoad_pct;Ssd_C;Chassis_C;Battery_pct;BatteryHealth_pct;BatteryStatus;HealthScore");

        RowsWritten = 0;
        CurrentLogSize = 0;
        IsLogging = true;
        _monitor.SnapshotUpdated += OnSnapshot;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        if (!IsLogging) return;
        _monitor.SnapshotUpdated -= OnSnapshot;
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Close();
            _writer?.Dispose();
            _writer = null;
        }
        IsLogging = false;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnSnapshot(object? sender, HardwareSnapshot s)
    {
        lock (_lock)
        {
            if (_writer is null) return;
            var c = CultureInfo.InvariantCulture;
            _writer.WriteLine(string.Join(";", new[]
            {
                s.Timestamp.ToString("o"),
                Fmt(s.CpuSilicon, c), Fmt(s.CpuPower, c), Fmt(s.CpuZone, c),
                Fmt(s.GpuZone, c),    Fmt(s.GpuLoad, c),  Fmt(s.Ssd, c),
                Fmt(s.ExtZone, c),    Fmt(s.BatteryLevel, c), Fmt(s.BatteryHealth, c),
                s.BatteryStatus ?? "",
                s.GlobalHealthScore.ToString(c)
            }));
            RowsWritten++;
            try { CurrentLogSize = new FileInfo(CurrentLogPath).Length; } catch { }
        }
        if (RowsWritten % 10 == 0) StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string Fmt(double? v, CultureInfo c) => v.HasValue ? v.Value.ToString("F2", c) : "";

    public void Dispose() => Stop();
}
