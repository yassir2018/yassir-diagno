namespace YassirDiagno.Models;

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
    string GlobalHealthLabel)
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
