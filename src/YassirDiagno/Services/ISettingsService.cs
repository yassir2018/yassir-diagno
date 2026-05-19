using System.IO;
using System.Text.Json;

namespace YassirDiagno.Services;

public sealed class AppSettings
{
    public string Theme { get; set; } = "Light";
    public int PollingIntervalSeconds { get; set; } = 1;
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool ShowSparklines { get; set; } = true;
}

public interface ISettingsService
{
    AppSettings Current { get; }
    void Save();
    event EventHandler? SettingsChanged;
    void NotifyChanged();
}

public sealed class SettingsService : ISettingsService
{
    private readonly string _path;
    public AppSettings Current { get; private set; }
    public event EventHandler? SettingsChanged;

    public SettingsService()
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YassirDiagno", "settings.json");
        Current = Load();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    public void NotifyChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
}
