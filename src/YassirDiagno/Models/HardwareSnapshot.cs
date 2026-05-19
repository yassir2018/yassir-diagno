namespace YassirDiagno.Models;

public record CpuCoreInfo(int Index, double? LoadPercent, double? ClockMHz);

public record GpuEngineLoad(string Name, double LoadPercent);

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
    IReadOnlyList<CpuCoreInfo>? Cores = null,
    double? RamUsagePercent = null,
    double? RamTotalGB = null,
    double? RamUsedGB = null,
    double? DiskFreeGB = null,
    double? DiskTotalGB = null,
    double? DiskFreePercent = null,
    double? SsdLifeRemaining = null,
    double? SsdAvailableSpare = null,
    double? GpuMemoryUsedMB = null,
    double? GpuMemoryTotalMB = null,
    IReadOnlyList<GpuEngineLoad>? GpuEngines = null)
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
