using YassirDiagno.Models;

namespace YassirDiagno.Services;

public enum HealthStatus { Excellent, Good, Warning, Critical, Unknown }

public sealed class ComponentHealth
{
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public required string Subtitle { get; init; }
    public required HealthStatus Status { get; init; }
    public required string StatusLabel { get; init; }
    public List<(string Key, string Value)> Metrics { get; init; } = new();
    public required string Verdict { get; init; }
    public string? Recommendation { get; init; }
}

public interface IComponentHealthService
{
    IReadOnlyList<ComponentHealth> Analyze(HardwareSnapshot snap, HardwareIdentity identity);
}

public sealed class ComponentHealthService : IComponentHealthService
{
    public IReadOnlyList<ComponentHealth> Analyze(HardwareSnapshot s, HardwareIdentity id) => new[]
    {
        AnalyzeCpu(s, id),
        AnalyzeRam(s),
        AnalyzeSsd(s, id),
        AnalyzeBattery(s, id),
        AnalyzeGpu(s, id),
        AnalyzeDiskSpace(s)
    };

    private static ComponentHealth AnalyzeCpu(HardwareSnapshot s, HardwareIdentity id)
    {
        var temp = s.CpuSilicon;
        var load = s.CpuTotalLoad;
        var clock = s.CpuAverageClock;

        HealthStatus status;
        string label, verdict;
        string? reco = null;

        if (temp is null) { status = HealthStatus.Unknown; label = "Inconnu"; verdict = "Données capteurs indisponibles."; }
        else if (temp >= 90)   { status = HealthStatus.Critical; label = "Surchauffe"; verdict = "Température critique, risque de throttling imminent."; reco = "Vérifier ventilation, nettoyer poussière, refaire pâte thermique."; }
        else if (temp >= 75)   { status = HealthStatus.Warning;  label = "Chaud";      verdict = "Température élevée. Le système peut limiter les performances."; reco = "Surveiller la charge, améliorer le refroidissement."; }
        else if (temp >= 55)   { status = HealthStatus.Good;     label = "Normal";     verdict = "Fonctionnement dans la plage normale."; }
        else                    { status = HealthStatus.Excellent; label = "Excellent"; verdict = "Température basse, refroidissement optimal."; }

        return new ComponentHealth
        {
            Name = "Processeur",
            Icon = "🖥",
            Subtitle = id.CpuName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Metrics = new()
            {
                ("Température silicon", temp is double t ? $"{t:F1} °C" : "--"),
                ("Charge totale",       load is double l ? $"{l:F1} %" : "--"),
                ("Fréquence moyenne",   clock is double c ? $"{c/1000:F2} GHz" : "--"),
                ("Puissance",           s.CpuPower is double p ? $"{p:F1} W" : "--")
            }
        };
    }

    private static ComponentHealth AnalyzeRam(HardwareSnapshot s)
    {
        var pct = s.RamUsagePercent;
        HealthStatus status;
        string label, verdict;
        string? reco = null;

        if (pct is null)         { status = HealthStatus.Unknown;   label = "Inconnu";       verdict = "Mesure indisponible."; }
        else if (pct >= 90)       { status = HealthStatus.Critical;  label = "Saturée";       verdict = "RAM presque pleine, performances dégradées."; reco = "Fermer applications inutilisées, redémarrer si nécessaire."; }
        else if (pct >= 75)       { status = HealthStatus.Warning;   label = "Bien utilisée"; verdict = "Utilisation élevée, marge réduite."; }
        else if (pct >= 50)       { status = HealthStatus.Good;      label = "Normale";       verdict = "Utilisation modérée, bonne marge disponible."; }
        else                       { status = HealthStatus.Excellent; label = "Disponible";    verdict = "Beaucoup de mémoire libre."; }

        return new ComponentHealth
        {
            Name = "Mémoire RAM",
            Icon = "💾",
            Subtitle = "Mémoire vive du système",
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Metrics = new()
            {
                ("Utilisation",          pct is double p ? $"{p:F0} %" : "--"),
                ("Utilisée",             s.RamUsedGB is double u ? $"{u:F1} GB" : "--"),
                ("Total installée",      s.RamTotalGB is double t ? $"{t:F1} GB" : "--"),
                ("Disponible",           (s.RamTotalGB - s.RamUsedGB) is double f ? $"{f:F1} GB" : "--")
            }
        };
    }

    private static ComponentHealth AnalyzeSsd(HardwareSnapshot s, HardwareIdentity id)
    {
        var temp = s.Ssd;
        var life = s.SsdLifeRemaining;
        var spare = s.SsdAvailableSpare;

        HealthStatus status;
        string label, verdict;
        string? reco = null;

        var worstScore = 100.0;
        if (temp is double t)
        {
            if (t >= 78) worstScore = Math.Min(worstScore, 20);
            else if (t >= 65) worstScore = Math.Min(worstScore, 50);
            else if (t >= 50) worstScore = Math.Min(worstScore, 75);
        }
        if (life is double l)
        {
            if (l < 50) worstScore = Math.Min(worstScore, 20);
            else if (l < 70) worstScore = Math.Min(worstScore, 50);
            else if (l < 85) worstScore = Math.Min(worstScore, 75);
        }

        if (worstScore >= 90)      { status = HealthStatus.Excellent; label = "Excellent"; verdict = "SSD en parfaite santé, températures fraîches."; }
        else if (worstScore >= 70) { status = HealthStatus.Good;      label = "Bon";       verdict = "SSD fonctionne normalement."; }
        else if (worstScore >= 40) { status = HealthStatus.Warning;   label = "Surveiller"; verdict = "Usure ou température notable. Surveiller l'évolution."; reco = "Éviter les écritures massives, vérifier ventilation."; }
        else                        { status = HealthStatus.Critical;  label = "Critique";  verdict = "SSD en fin de vie ou surchauffe critique."; reco = "Sauvegarder les données importantes et envisager remplacement."; }

        return new ComponentHealth
        {
            Name = "Stockage SSD",
            Icon = "💿",
            Subtitle = id.SsdName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Metrics = new()
            {
                ("Température",       temp is double t1 ? $"{t1:F1} °C" : "--"),
                ("Vie restante",      life is double lr ? $"{lr:F0} %" : "--"),
                ("Spare disponible",  spare is double sp ? $"{sp:F0} %" : "--"),
                ("Seuil critique",    "83 °C (vendor)")
            }
        };
    }

    private static ComponentHealth AnalyzeBattery(HardwareSnapshot s, HardwareIdentity id)
    {
        var charge = s.BatteryLevel;
        var health = s.BatteryHealth;
        var statusStr = s.BatteryStatus;

        HealthStatus status;
        string label, verdict;
        string? reco = null;

        if (health is null) { status = HealthStatus.Unknown; label = "Inconnue"; verdict = "Données batterie indisponibles."; }
        else if (health < 50) { status = HealthStatus.Critical; label = "À remplacer"; verdict = $"Santé {health:F0}% — autonomie sérieusement réduite."; reco = "Remplacement de la batterie recommandé."; }
        else if (health < 70) { status = HealthStatus.Warning;  label = "Dégradée";    verdict = $"Santé {health:F0}% — usure notable."; reco = "Surveiller, prévoir remplacement dans les prochains mois."; }
        else if (health < 85) { status = HealthStatus.Good;     label = "Correcte";    verdict = $"Santé {health:F0}% — légère usure normale."; }
        else                   { status = HealthStatus.Excellent;label = "Excellente";  verdict = $"Santé {health:F0}% — proche du neuf."; }

        return new ComponentHealth
        {
            Name = "Batterie",
            Icon = "🔋",
            Subtitle = id.BatteryName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Metrics = new()
            {
                ("Charge actuelle",  charge is double c ? $"{c:F0} %" : "--"),
                ("Santé",            health is double h ? $"{h:F0} %" : "--"),
                ("Statut",           statusStr ?? "--"),
                ("Capacité perdue",  health is double hh ? $"~{100-hh:F0} %" : "--")
            }
        };
    }

    private static ComponentHealth AnalyzeGpu(HardwareSnapshot s, HardwareIdentity id)
    {
        var temp = s.GpuZone;
        var load = s.GpuLoad;

        HealthStatus status;
        string label, verdict;
        string? reco = null;

        if (load is null) { status = HealthStatus.Unknown; label = "Inconnu"; verdict = "Données GPU indisponibles."; }
        else if (load >= 90 || (temp ?? 0) >= 85) { status = HealthStatus.Critical; label = "Saturé"; verdict = "GPU à pleine charge ou très chaud."; reco = "Réduire la charge graphique."; }
        else if (load >= 70 || (temp ?? 0) >= 70) { status = HealthStatus.Warning;  label = "Élevé";  verdict = "Activité graphique intense."; }
        else if (load >= 30)                       { status = HealthStatus.Good;     label = "Modéré"; verdict = "Activité graphique normale."; }
        else                                        { status = HealthStatus.Excellent; label = "Idle";  verdict = "GPU au repos."; }

        var memUsed = s.GpuMemoryUsedMB;
        var memTotal = s.GpuMemoryTotalMB;
        var memPct = (memUsed is double mu && memTotal is double mt && mt > 0) ? mu / mt * 100 : (double?)null;

        return new ComponentHealth
        {
            Name = "Carte graphique",
            Icon = "🎮",
            Subtitle = id.GpuName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Metrics = new()
            {
                ("Charge",      load is double l ? $"{l:F0} %" : "--"),
                ("Température", temp is double t ? $"{t:F0} °C" : "--"),
                ("VRAM utilisée", memUsed is double mu2 ? $"{mu2:F0} MB" : "--"),
                ("VRAM totale",   memTotal is double mt2 ? $"{mt2:F0} MB" : "--")
            }
        };
    }

    private static ComponentHealth AnalyzeDiskSpace(HardwareSnapshot s)
    {
        var freePct = s.DiskFreePercent;
        HealthStatus status;
        string label, verdict;
        string? reco = null;

        if (freePct is null)        { status = HealthStatus.Unknown;   label = "Inconnu";   verdict = "Données disque indisponibles."; }
        else if (freePct < 7)        { status = HealthStatus.Critical;  label = "Critique";  verdict = "Disque presque plein, Windows pourrait dysfonctionner."; reco = "Libérer urgentement de l'espace (Disk Cleanup, désinstaller apps)."; }
        else if (freePct < 15)       { status = HealthStatus.Warning;   label = "Attention"; verdict = "Espace disque faible."; reco = "Libérer de l'espace pour éviter les ralentissements."; }
        else if (freePct < 30)       { status = HealthStatus.Good;      label = "Correct";   verdict = "Espace disque suffisant."; }
        else                          { status = HealthStatus.Excellent; label = "Excellent"; verdict = "Beaucoup d'espace disponible."; }

        return new ComponentHealth
        {
            Name = "Espace disque",
            Icon = "📂",
            Subtitle = "Lecteur système (C:)",
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Metrics = new()
            {
                ("Libre",        s.DiskFreeGB is double f ? $"{f:F1} GB" : "--"),
                ("Total",        s.DiskTotalGB is double t ? $"{t:F1} GB" : "--"),
                ("Utilisé",      (s.DiskTotalGB - s.DiskFreeGB) is double u ? $"{u:F1} GB" : "--"),
                ("Libre %",      freePct is double p ? $"{p:F1} %" : "--")
            }
        };
    }
}
