using System.Collections.ObjectModel;
using System.Management;

namespace YassirDiagno.Services.Hardware;

public record HardwareItem(string Category, string Name, string Detail);

public interface IHardwareInventoryService
{
    Task<ObservableCollection<HardwareItem>> GetInventoryAsync();
}

public sealed class HardwareInventoryService : IHardwareInventoryService
{
    public Task<ObservableCollection<HardwareItem>> GetInventoryAsync() => Task.Run(() =>
    {
        var list = new ObservableCollection<HardwareItem>();

        TryQuery("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize, Architecture FROM Win32_Processor",
            obj =>
            {
                var name = obj["Name"]?.ToString()?.Trim();
                var cores = obj["NumberOfCores"];
                var threads = obj["NumberOfLogicalProcessors"];
                var clock = obj["MaxClockSpeed"];
                var l2 = obj["L2CacheSize"];
                var l3 = obj["L3CacheSize"];
                list.Add(new HardwareItem("CPU", name ?? "Processeur",
                    $"{cores} cœurs · {threads} threads · {clock} MHz · L2 {l2} KB · L3 {l3} KB"));
            });

        TryQuery("SELECT Name, AdapterRAM, DriverVersion, VideoProcessor FROM Win32_VideoController",
            obj =>
            {
                var name = obj["Name"]?.ToString()?.Trim();
                var ram = Convert.ToInt64(obj["AdapterRAM"] ?? 0L) / 1024 / 1024;
                var driver = obj["DriverVersion"]?.ToString();
                list.Add(new HardwareItem("GPU", name ?? "Carte graphique",
                    $"VRAM {ram} MB · Driver {driver}"));
            });

        try
        {
            using var s = new ManagementObjectSearcher("SELECT Capacity, Speed, Manufacturer, PartNumber, DeviceLocator FROM Win32_PhysicalMemory");
            int slot = 0;
            foreach (var obj in s.Get())
            {
                slot++;
                var cap = Convert.ToInt64(obj["Capacity"] ?? 0L) / 1024 / 1024 / 1024;
                var speed = obj["Speed"];
                var manu = obj["Manufacturer"]?.ToString()?.Trim();
                var part = obj["PartNumber"]?.ToString()?.Trim();
                var loc = obj["DeviceLocator"]?.ToString();
                list.Add(new HardwareItem("RAM", $"Slot {slot} — {loc}",
                    $"{cap} GB · {speed} MHz · {manu} {part}"));
            }
        }
        catch { }

        TryQuery("SELECT Model, Size, MediaType, InterfaceType, SerialNumber FROM Win32_DiskDrive",
            obj =>
            {
                var model = obj["Model"]?.ToString()?.Trim();
                var size = Convert.ToInt64(obj["Size"] ?? 0L) / 1024 / 1024 / 1024;
                var media = obj["MediaType"]?.ToString();
                var iface = obj["InterfaceType"]?.ToString();
                list.Add(new HardwareItem("Stockage", model ?? "Disque",
                    $"{size} GB · {iface} · {media}"));
            });

        TryQuery("SELECT Name, Manufacturer, AdapterType, MACAddress, NetEnabled FROM Win32_NetworkAdapter WHERE PhysicalAdapter=true",
            obj =>
            {
                var name = obj["Name"]?.ToString()?.Trim();
                var manu = obj["Manufacturer"]?.ToString()?.Trim();
                var type = obj["AdapterType"]?.ToString();
                var mac = obj["MACAddress"]?.ToString();
                var enabled = (bool?)obj["NetEnabled"] ?? false;
                list.Add(new HardwareItem("Réseau", name ?? "Adaptateur",
                    $"{manu} · {type} · {mac} · {(enabled ? "actif" : "inactif")}"));
            });

        TryQuery("SELECT Name, EstimatedChargeRemaining, DesignCapacity, FullChargeCapacity FROM Win32_Battery",
            obj =>
            {
                var name = obj["Name"]?.ToString()?.Trim();
                var charge = obj["EstimatedChargeRemaining"];
                list.Add(new HardwareItem("Batterie", name ?? "Battery",
                    $"Charge actuelle: {charge}%"));
            });

        TryQuery("SELECT Manufacturer, Product, Version, SerialNumber FROM Win32_BaseBoard",
            obj =>
            {
                var manu = obj["Manufacturer"]?.ToString()?.Trim();
                var prod = obj["Product"]?.ToString()?.Trim();
                var ver = obj["Version"]?.ToString()?.Trim();
                list.Add(new HardwareItem("Carte mère", $"{manu} {prod}",
                    $"Version {ver}"));
            });

        return list;
    });

    private static void TryQuery(string wql, Action<ManagementObject> handler)
    {
        try
        {
            using var s = new ManagementObjectSearcher(wql);
            foreach (var obj in s.Get())
            {
                try { handler((ManagementObject)obj); } catch { }
            }
        }
        catch { }
    }
}
