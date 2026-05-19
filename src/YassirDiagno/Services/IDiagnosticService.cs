using YassirDiagno.Models;
using YassirDiagno.Services.Hardware;

namespace YassirDiagno.Services;

public enum DiagnosticSeverity { Good, Info, Warning, Critical }

public record DiagnosticFinding(DiagnosticSeverity Severity, string Title, string Detail, string? Recommendation = null);

public interface IDiagnosticService
{
    Task<List<DiagnosticFinding>> RunQuickScanAsync();
}

public sealed class DiagnosticService : IDiagnosticService
{
    private readonly IHardwareMonitorService _monitor;
    private readonly ISystemInfoService _system;

    public DiagnosticService(IHardwareMonitorService monitor, ISystemInfoService system)
    {
        _monitor = monitor;
        _system = system;
    }

    public async Task<List<DiagnosticFinding>> RunQuickScanAsync()
    {
        await _monitor.InitializeAsync();
        var snap = _monitor.ReadSnapshot();
        var sysInfo = await _system.GetSystemInfoAsync();
        var findings = new List<DiagnosticFinding>();

        if (snap.CpuSilicon is double cpu)
        {
            if (cpu >= 90) findings.Add(new(DiagnosticSeverity.Critical, "CPU surchauffe critique", $"Température silicon {cpu:F1}°C, proche du throttling.", "Nettoyer ventilateurs, remplacer pâte thermique, réduire charge."));
            else if (cpu >= 75) findings.Add(new(DiagnosticSeverity.Warning, "CPU chaud", $"Température silicon {cpu:F1}°C élevée pour usage normal.", "Surveiller la charge, vérifier ventilation."));
            else findings.Add(new(DiagnosticSeverity.Good, "CPU température normale", $"Température silicon {cpu:F1}°C dans la plage saine."));
        }
        else findings.Add(new(DiagnosticSeverity.Info, "CPU température", "Non disponible (driver/permissions)."));

        if (snap.Ssd is double ssd)
        {
            if (ssd >= 75) findings.Add(new(DiagnosticSeverity.Critical, "SSD très chaud", $"Température {ssd:F1}°C proche du seuil de throttling NVMe (78°C).", "Améliorer ventilation, ajouter dissipateur SSD."));
            else if (ssd >= 60) findings.Add(new(DiagnosticSeverity.Warning, "SSD chaud", $"Température {ssd:F1}°C élevée.", "Surveiller les écritures intensives."));
            else findings.Add(new(DiagnosticSeverity.Good, "SSD température normale", $"Température {ssd:F1}°C correcte."));
        }

        if (snap.BatteryHealth is double bh)
        {
            if (bh < 50) findings.Add(new(DiagnosticSeverity.Critical, "Batterie dégradée", $"Santé {bh:F0}% — autonomie sérieusement réduite.", "Envisager remplacement de la batterie."));
            else if (bh < 70) findings.Add(new(DiagnosticSeverity.Warning, "Batterie usée", $"Santé {bh:F0}% — dégradation notable.", "Surveiller, remplacer dans les prochains mois."));
            else if (bh < 85) findings.Add(new(DiagnosticSeverity.Info, "Batterie correcte", $"Santé {bh:F0}% — légère usure normale."));
            else findings.Add(new(DiagnosticSeverity.Good, "Batterie en bonne santé", $"Santé {bh:F0}% — proche du neuf."));
        }

        if (snap.CpuSilicon is double cs && snap.ExtZone is double ext)
        {
            var delta = cs - ext;
            if (delta > 35) findings.Add(new(DiagnosticSeverity.Warning, "Écart thermique élevé", $"Δ {delta:F1}°C entre silicon et chassis — possible pâte thermique sèche ou ventilateur poussiéreux.", "Maintenance préventive recommandée."));
            else if (delta > 25) findings.Add(new(DiagnosticSeverity.Info, "Écart thermique modéré", $"Δ {delta:F1}°C — cooling fonctionne mais peut être amélioré."));
            else findings.Add(new(DiagnosticSeverity.Good, "Écart thermique optimal", $"Δ {delta:F1}°C — cooling efficace."));
        }

        if (sysInfo.RamUsagePercent > 90) findings.Add(new(DiagnosticSeverity.Warning, "RAM saturée", $"{sysInfo.RamUsagePercent:F0}% utilisée.", "Fermer les applications inutilisées."));
        else if (sysInfo.RamUsagePercent > 75) findings.Add(new(DiagnosticSeverity.Info, "RAM bien utilisée", $"{sysInfo.RamUsagePercent:F0}% utilisée."));
        else findings.Add(new(DiagnosticSeverity.Good, "RAM disponible", $"{sysInfo.RamUsagePercent:F0}% utilisée — bonne marge."));

        findings.Add(new(DiagnosticSeverity.Info, "Score santé globale", $"{snap.GlobalHealthScore}% — {snap.GlobalHealthLabel}"));

        return findings;
    }
}
