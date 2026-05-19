using Microsoft.Toolkit.Uwp.Notifications;
using YassirDiagno.Models;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.Services;

public interface INotificationService : IDisposable
{
    void Start();
    void Stop();
    void ShowToast(string title, string message);
}

public sealed class NotificationService : INotificationService
{
    private readonly IHardwareMonitorService _monitor;
    private readonly ISettingsService _settings;
    private DateTime _lastCpuAlert = DateTime.MinValue;
    private DateTime _lastSsdAlert = DateTime.MinValue;
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromMinutes(5);

    public NotificationService(IHardwareMonitorService monitor, ISettingsService settings)
    {
        _monitor = monitor;
        _settings = settings;
    }

    public void Start() => _monitor.SnapshotUpdated += OnSnapshot;
    public void Stop() => _monitor.SnapshotUpdated -= OnSnapshot;

    private void OnSnapshot(object? sender, HardwareSnapshot snap)
    {
        if (!_settings.Current.NotificationsEnabled) return;
        var now = DateTime.Now;

        if (snap.CpuSilicon is double cpu && cpu >= _settings.Current.CpuTempCriticalThreshold)
        {
            if (now - _lastCpuAlert > AlertCooldown)
            {
                _lastCpuAlert = now;
                ShowToast("⚠ CPU surchauffe critique", $"Température silicon: {cpu:F1}°C — Risque de throttling imminent.");
            }
        }
        if (snap.Ssd is double ssd && ssd >= _settings.Current.SsdTempCriticalThreshold)
        {
            if (now - _lastSsdAlert > AlertCooldown)
            {
                _lastSsdAlert = now;
                ShowToast("⚠ SSD chaud", $"Température composite: {ssd:F1}°C — Proche du seuil de throttling NVMe.");
            }
        }
    }

    public void ShowToast(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch { }
    }

    public void Dispose() => Stop();
}
