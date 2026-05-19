using YassirDiagno.Models;

namespace YassirDiagno.Services.Hardware;

public interface IHardwareMonitorService : IDisposable
{
    Task InitializeAsync(CancellationToken ct = default);
    HardwareIdentity GetIdentity();
    HardwareSnapshot ReadSnapshot();
    event EventHandler<HardwareSnapshot>? SnapshotUpdated;
    void StartPolling(TimeSpan interval);
    void StopPolling();
}
