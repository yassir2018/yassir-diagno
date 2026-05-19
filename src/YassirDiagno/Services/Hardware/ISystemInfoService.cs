using System.Management;
using System.Runtime.InteropServices;

namespace YassirDiagno.Services.Hardware;

public record SystemInfo(
    string OsName,
    string OsVersion,
    string OsArchitecture,
    string OsInstallDate,
    string Hostname,
    string Username,
    string Uptime,
    double TotalRamGB,
    double UsedRamGB,
    double FreeRamGB,
    double RamUsagePercent,
    string BiosManufacturer,
    string BiosVersion,
    string BiosReleaseDate,
    string CpuArchitecture,
    int LogicalProcessors,
    string SystemModel,
    string SystemManufacturer);

public interface ISystemInfoService
{
    Task<SystemInfo> GetSystemInfoAsync();
}

public sealed class SystemInfoService : ISystemInfoService
{
    public Task<SystemInfo> GetSystemInfoAsync() => Task.Run(() =>
    {
        string osName = "Windows", osVersion = "?", osArch = "?", osInstall = "?";
        double totalRam = 0, freeRam = 0;
        string hostname = Environment.MachineName;
        string username = Environment.UserName;
        string uptime = FormatUptime(TimeSpan.FromMilliseconds(Environment.TickCount64));

        try
        {
            using var s = new ManagementObjectSearcher("SELECT Caption, Version, OSArchitecture, InstallDate, TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (var obj in s.Get())
            {
                osName = obj["Caption"]?.ToString()?.Trim() ?? osName;
                osVersion = obj["Version"]?.ToString() ?? osVersion;
                osArch = obj["OSArchitecture"]?.ToString() ?? osArch;
                var installRaw = obj["InstallDate"]?.ToString();
                if (!string.IsNullOrEmpty(installRaw) && installRaw.Length >= 8)
                {
                    try
                    {
                        var dt = ManagementDateTimeConverter.ToDateTime(installRaw);
                        osInstall = dt.ToString("dd MMM yyyy");
                    }
                    catch { }
                }
                totalRam = Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024 / 1024;
                freeRam = Convert.ToDouble(obj["FreePhysicalMemory"]) / 1024 / 1024;
            }
        }
        catch { }

        string bios = "?", biosVer = "?", biosDate = "?";
        try
        {
            using var s = new ManagementObjectSearcher("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
            foreach (var obj in s.Get())
            {
                bios = obj["Manufacturer"]?.ToString() ?? bios;
                biosVer = obj["SMBIOSBIOSVersion"]?.ToString() ?? biosVer;
                var raw = obj["ReleaseDate"]?.ToString();
                if (!string.IsNullOrEmpty(raw) && raw.Length >= 8)
                {
                    try
                    {
                        var dt = ManagementDateTimeConverter.ToDateTime(raw);
                        biosDate = dt.ToString("dd MMM yyyy");
                    }
                    catch { }
                }
            }
        }
        catch { }

        string manu = "?", model = "?";
        int logicalProcs = Environment.ProcessorCount;
        try
        {
            using var s = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
            foreach (var obj in s.Get())
            {
                manu = obj["Manufacturer"]?.ToString() ?? manu;
                model = obj["Model"]?.ToString() ?? model;
            }
        }
        catch { }

        var arch = RuntimeInformation.OSArchitecture.ToString();
        var usedRam = totalRam - freeRam;
        var usagePct = totalRam > 0 ? (usedRam / totalRam) * 100 : 0;

        return new SystemInfo(
            osName, osVersion, osArch, osInstall,
            hostname, username, uptime,
            Math.Round(totalRam, 2), Math.Round(usedRam, 2), Math.Round(freeRam, 2), Math.Round(usagePct, 1),
            bios, biosVer, biosDate,
            arch, logicalProcs,
            model, manu);
    });

    private static string FormatUptime(TimeSpan t) =>
        $"{(int)t.TotalDays}j {t.Hours}h {t.Minutes}m";
}
