using System.Diagnostics;
using Microsoft.Win32;

namespace YassirDiagno.Services;

public interface IAutoStartService
{
    bool IsEnabled();
    void SetEnabled(bool enabled);
}

public sealed class AutoStartService : IAutoStartService
{
    private const string AppName = "YassirDiagno";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(AppName) is not null;
        }
        catch { return false; }
    }

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true)
                ?? Registry.CurrentUser.CreateSubKey(RunKey, true);
            if (key is null) return;

            if (enabled)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exePath))
                    key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                if (key.GetValue(AppName) is not null)
                    key.DeleteValue(AppName, false);
            }
        }
        catch { }
    }
}
