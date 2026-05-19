namespace YassirDiagno.Models;

public record CpuCoreInfo(int Index, double? LoadPercent, double? ClockMHz);

public record HardwareSnapshot(
    DateTime Timestamp,
    double? CpuSilicon,
    double? CpuPower,
    double? CpuZone,
    double? GpuZone,
    double? GpuLoad,
    double? Ssd,
    double? ExtZone,
    double? BatteryLevel,
    double? BatteryHealth,
    string? BatteryStatus,
    int GlobalHealthScore,
    string GlobalHealthLabel,
    double? CpuTotalLoad = null,
    double? CpuAverageClock = null,
    IReadOnlyList<CpuCoreInfo>? Cores = null)
{
    public static HardwareSnapshot Empty() => new(
        DateTime.Now, null, null, null, null, null, null, null, null, null, null, 100, "BON");
}

public record HardwareIdentity(
    string CpuName,
    string GpuName,
    string SsdName,
    string BatteryName,
    string MotherboardName);
