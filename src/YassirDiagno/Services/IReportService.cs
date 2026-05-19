using System.Text;
using YassirDiagno.Models;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.Services;

public interface IReportService
{
    Task<string> GenerateTextReportAsync();
    Task<string> GenerateHtmlReportAsync();
}

public sealed class ReportService : IReportService
{
    private readonly IHardwareMonitorService _monitor;
    private readonly ISystemInfoService _system;
    private readonly IHardwareInventoryService _inventory;
    private readonly IDiagnosticService _diagnostic;

    public ReportService(IHardwareMonitorService monitor, ISystemInfoService system,
                         IHardwareInventoryService inventory, IDiagnosticService diagnostic)
    {
        _monitor = monitor;
        _system = system;
        _inventory = inventory;
        _diagnostic = diagnostic;
    }

    public async Task<string> GenerateTextReportAsync()
    {
        await _monitor.InitializeAsync();
        var snap = _monitor.ReadSnapshot();
        var id = _monitor.GetIdentity();
        var sys = await _system.GetSystemInfoAsync();
        var inv = await _inventory.GetInventoryAsync();
        var diag = await _diagnostic.RunQuickScanAsync();

        var sb = new StringBuilder();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                    YASSIR DIAGNO - RAPPORT                    ");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Date du rapport : {DateTime.Now:dd MMMM yyyy HH:mm:ss}");
        sb.AppendLine($"Machine        : {sys.SystemManufacturer} {sys.SystemModel}");
        sb.AppendLine($"Hostname       : {sys.Hostname}");
        sb.AppendLine($"Utilisateur    : {sys.Username}");
        sb.AppendLine();

        sb.AppendLine("─── SYSTÈME ───────────────────────────────────────────────────");
        sb.AppendLine($"OS             : {sys.OsName}");
        sb.AppendLine($"Version        : {sys.OsVersion}");
        sb.AppendLine($"Architecture   : {sys.OsArchitecture}");
        sb.AppendLine($"Installation   : {sys.OsInstallDate}");
        sb.AppendLine($"Uptime         : {sys.Uptime}");
        sb.AppendLine($"RAM            : {sys.UsedRamGB:F1} / {sys.TotalRamGB:F1} GB ({sys.RamUsagePercent:F0}%)");
        sb.AppendLine($"BIOS           : {sys.BiosManufacturer} {sys.BiosVersion} ({sys.BiosReleaseDate})");
        sb.AppendLine();

        sb.AppendLine("─── ÉTAT THERMIQUE ──────────────────────────────────────────");
        sb.AppendLine($"CPU silicon    : {(snap.CpuSilicon?.ToString("F1") ?? "--")} °C");
        sb.AppendLine($"CPU package    : {(snap.CpuZone?.ToString("F1") ?? "--")} °C");
        sb.AppendLine($"GPU zone       : {(snap.GpuZone?.ToString("F1") ?? "--")} °C");
        sb.AppendLine($"GPU load       : {(snap.GpuLoad?.ToString("F0") ?? "--")} %");
        sb.AppendLine($"SSD            : {(snap.Ssd?.ToString("F1") ?? "--")} °C");
        sb.AppendLine($"Chassis        : {(snap.ExtZone?.ToString("F1") ?? "--")} °C");
        sb.AppendLine($"CPU power      : {(snap.CpuPower?.ToString("F1") ?? "--")} W");
        sb.AppendLine();

        sb.AppendLine("─── BATTERIE ─────────────────────────────────────────────────");
        sb.AppendLine($"Modèle         : {id.BatteryName}");
        sb.AppendLine($"Charge         : {(snap.BatteryLevel?.ToString("F0") ?? "--")} %");
        sb.AppendLine($"Santé          : {(snap.BatteryHealth?.ToString("F0") ?? "--")} %");
        sb.AppendLine($"Statut         : {snap.BatteryStatus ?? "--"}");
        sb.AppendLine();

        sb.AppendLine("─── SANTÉ GLOBALE ───────────────────────────────────────────");
        sb.AppendLine($"Score          : {snap.GlobalHealthScore} % ({snap.GlobalHealthLabel})");
        sb.AppendLine();

        sb.AppendLine("─── INVENTAIRE MATÉRIEL ─────────────────────────────────────");
        foreach (var grp in inv.GroupBy(x => x.Category))
        {
            sb.AppendLine();
            sb.AppendLine($"[{grp.Key}]");
            foreach (var item in grp)
            {
                sb.AppendLine($"  • {item.Name}");
                sb.AppendLine($"      {item.Detail}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("─── DIAGNOSTIC ──────────────────────────────────────────────");
        foreach (var f in diag)
        {
            var icon = f.Severity switch
            {
                DiagnosticSeverity.Good => "✓",
                DiagnosticSeverity.Info => "ⓘ",
                DiagnosticSeverity.Warning => "⚠",
                DiagnosticSeverity.Critical => "✕",
                _ => "·"
            };
            sb.AppendLine($"{icon}  {f.Title}");
            sb.AppendLine($"     {f.Detail}");
            if (!string.IsNullOrWhiteSpace(f.Recommendation))
                sb.AppendLine($"     → {f.Recommendation}");
            sb.AppendLine();
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Généré par Yassir Diagno v1.0.0 le {DateTime.Now:dd/MM/yyyy à HH:mm}");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    public async Task<string> GenerateHtmlReportAsync()
    {
        await _monitor.InitializeAsync();
        var snap = _monitor.ReadSnapshot();
        var id = _monitor.GetIdentity();
        var sys = await _system.GetSystemInfoAsync();
        var inv = await _inventory.GetInventoryAsync();
        var diag = await _diagnostic.RunQuickScanAsync();

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"fr\"><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<title>Yassir Diagno — Rapport</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:'Segoe UI',sans-serif;max-width:900px;margin:30px auto;padding:0 20px;color:#111827;background:#F5F7FA}");
        sb.AppendLine("h1{color:#3B82F6;border-bottom:3px solid #3B82F6;padding-bottom:10px}");
        sb.AppendLine("h2{color:#111827;margin-top:30px;font-size:16px;border-bottom:1px solid #E5E7EB;padding-bottom:6px}");
        sb.AppendLine(".card{background:#fff;border:1px solid #E5E7EB;border-radius:12px;padding:18px;margin:12px 0;box-shadow:0 1px 3px rgba(0,0,0,0.04)}");
        sb.AppendLine(".kv{display:grid;grid-template-columns:180px 1fr;gap:8px;font-size:13px}");
        sb.AppendLine(".kv .k{color:#6B7280}.kv .v{color:#111827;font-weight:500}");
        sb.AppendLine(".finding{display:flex;gap:12px;padding:10px;background:#FAFBFC;border-radius:6px;margin:6px 0}");
        sb.AppendLine(".sev-good{color:#10B981}.sev-info{color:#3B82F6}.sev-warn{color:#F59E0B}.sev-crit{color:#EF4444}");
        sb.AppendLine(".badge{display:inline-block;padding:3px 8px;background:#EFF6FF;color:#3B82F6;border-radius:4px;font-size:10px;font-weight:bold;margin-right:8px}");
        sb.AppendLine(".footer{text-align:center;color:#9CA3AF;font-size:11px;margin-top:40px;padding-top:20px;border-top:1px solid #E5E7EB}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine($"<h1>Yassir Diagno — Rapport de diagnostic</h1>");
        sb.AppendLine($"<p style='color:#6B7280;font-size:12px'>Généré le {DateTime.Now:dd MMMM yyyy à HH:mm:ss}</p>");

        sb.AppendLine("<div class='card'><h2>Système</h2><div class='kv'>");
        sb.AppendLine($"<div class='k'>Machine</div><div class='v'>{sys.SystemManufacturer} {sys.SystemModel}</div>");
        sb.AppendLine($"<div class='k'>Hostname</div><div class='v'>{sys.Hostname}</div>");
        sb.AppendLine($"<div class='k'>OS</div><div class='v'>{sys.OsName} ({sys.OsArchitecture})</div>");
        sb.AppendLine($"<div class='k'>Version</div><div class='v'>{sys.OsVersion}</div>");
        sb.AppendLine($"<div class='k'>Uptime</div><div class='v'>{sys.Uptime}</div>");
        sb.AppendLine($"<div class='k'>RAM</div><div class='v'>{sys.UsedRamGB:F1} / {sys.TotalRamGB:F1} GB ({sys.RamUsagePercent:F0}%)</div>");
        sb.AppendLine($"<div class='k'>BIOS</div><div class='v'>{sys.BiosManufacturer} {sys.BiosVersion} ({sys.BiosReleaseDate})</div>");
        sb.AppendLine("</div></div>");

        sb.AppendLine("<div class='card'><h2>État thermique</h2><div class='kv'>");
        sb.AppendLine($"<div class='k'>CPU silicon</div><div class='v'>{snap.CpuSilicon?.ToString("F1") ?? "--"} °C</div>");
        sb.AppendLine($"<div class='k'>CPU package</div><div class='v'>{snap.CpuZone?.ToString("F1") ?? "--"} °C</div>");
        sb.AppendLine($"<div class='k'>GPU zone</div><div class='v'>{snap.GpuZone?.ToString("F1") ?? "--"} °C</div>");
        sb.AppendLine($"<div class='k'>SSD</div><div class='v'>{snap.Ssd?.ToString("F1") ?? "--"} °C</div>");
        sb.AppendLine($"<div class='k'>Chassis</div><div class='v'>{snap.ExtZone?.ToString("F1") ?? "--"} °C</div>");
        sb.AppendLine($"<div class='k'>CPU power</div><div class='v'>{snap.CpuPower?.ToString("F1") ?? "--"} W</div>");
        sb.AppendLine("</div></div>");

        sb.AppendLine("<div class='card'><h2>Batterie & Santé globale</h2><div class='kv'>");
        sb.AppendLine($"<div class='k'>Modèle</div><div class='v'>{id.BatteryName}</div>");
        sb.AppendLine($"<div class='k'>Charge</div><div class='v'>{snap.BatteryLevel?.ToString("F0") ?? "--"} %</div>");
        sb.AppendLine($"<div class='k'>Santé batterie</div><div class='v'>{snap.BatteryHealth?.ToString("F0") ?? "--"} %</div>");
        sb.AppendLine($"<div class='k'>Statut</div><div class='v'>{snap.BatteryStatus ?? "--"}</div>");
        sb.AppendLine($"<div class='k'>Santé globale PC</div><div class='v'><strong>{snap.GlobalHealthScore}% — {snap.GlobalHealthLabel}</strong></div>");
        sb.AppendLine("</div></div>");

        sb.AppendLine("<div class='card'><h2>Inventaire matériel</h2>");
        foreach (var grp in inv.GroupBy(x => x.Category))
        {
            sb.AppendLine($"<h3 style='font-size:13px;color:#6B7280;margin-top:14px'>{grp.Key}</h3>");
            foreach (var item in grp)
            {
                sb.AppendLine($"<div style='margin:6px 0'><span class='badge'>{grp.Key}</span><strong>{item.Name}</strong><br><span style='color:#6B7280;font-size:11px'>{item.Detail}</span></div>");
            }
        }
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='card'><h2>Diagnostic</h2>");
        foreach (var f in diag)
        {
            var sevClass = f.Severity switch
            {
                DiagnosticSeverity.Good => "sev-good",
                DiagnosticSeverity.Info => "sev-info",
                DiagnosticSeverity.Warning => "sev-warn",
                DiagnosticSeverity.Critical => "sev-crit",
                _ => ""
            };
            var icon = f.Severity switch
            {
                DiagnosticSeverity.Good => "✓",
                DiagnosticSeverity.Info => "ⓘ",
                DiagnosticSeverity.Warning => "⚠",
                DiagnosticSeverity.Critical => "✕",
                _ => "·"
            };
            sb.AppendLine($"<div class='finding'><div class='{sevClass}' style='font-size:20px;font-weight:bold'>{icon}</div>");
            sb.AppendLine($"<div><strong>{f.Title}</strong><br><span style='color:#6B7280;font-size:12px'>{f.Detail}</span>");
            if (!string.IsNullOrWhiteSpace(f.Recommendation))
                sb.AppendLine($"<br><em style='color:#9CA3AF;font-size:11px'>→ {f.Recommendation}</em>");
            sb.AppendLine("</div></div>");
        }
        sb.AppendLine("</div>");

        sb.AppendLine("<div class='footer'>Généré par Yassir Diagno v1.0.0</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }
}
