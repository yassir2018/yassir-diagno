using System.Diagnostics.Eventing.Reader;
using System.Management;
using System.Net.NetworkInformation;
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
    private DateTime _slowCacheTime = DateTime.MinValue;
    private ComponentHealth? _cachedDefender;
    private ComponentHealth? _cachedThermal;
    private static readonly TimeSpan SlowCacheTtl = TimeSpan.FromSeconds(60);

    public IReadOnlyList<ComponentHealth> Analyze(HardwareSnapshot s, HardwareIdentity id)
    {
        if (DateTime.Now - _slowCacheTime > SlowCacheTtl)
        {
            _cachedDefender = AnalyzeDefender();
            _cachedThermal = AnalyzeThermalEvents();
            _slowCacheTime = DateTime.Now;
        }

        return new[]
        {
            AnalyzeCpu(s, id),
            AnalyzeRam(s),
            AnalyzeSsd(s, id),
            AnalyzeBattery(s, id),
            AnalyzeGpu(s, id),
            AnalyzeDiskSpace(s),
            _cachedDefender ?? FallbackUnknown("Windows Defender", "🛡", "Antivirus / Sécurité"),
            AnalyzeNetwork(),
            _cachedThermal ?? FallbackUnknown("Événements thermiques", "🔥", "Hibernations / overheating récents")
        };
    }

    private static ComponentHealth FallbackUnknown(string name, string icon, string subtitle) => new()
    {
        Name = name, Icon = icon, Subtitle = subtitle,
        Status = HealthStatus.Unknown, StatusLabel = "Inconnu",
        Verdict = "Données indisponibles."
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

    private static ComponentHealth AnalyzeDefender()
    {
        try
        {
            using var s = new ManagementObjectSearcher(
                @"root\Microsoft\Windows\Defender",
                "SELECT AntivirusEnabled, RealTimeProtectionEnabled, AntivirusSignatureAge, QuickScanStartTime, AMServiceEnabled FROM MSFT_MpComputerStatus");

            foreach (var obj in s.Get())
            {
                var avEnabled = (bool?)obj["AntivirusEnabled"] ?? false;
                var rtEnabled = (bool?)obj["RealTimeProtectionEnabled"] ?? false;
                var amService = (bool?)obj["AMServiceEnabled"] ?? false;
                var sigAge = obj["AntivirusSignatureAge"] is uint a ? a : 999;
                var lastScanRaw = obj["QuickScanStartTime"]?.ToString();
                string lastScan = "Jamais";
                try { if (!string.IsNullOrEmpty(lastScanRaw)) lastScan = ManagementDateTimeConverter.ToDateTime(lastScanRaw).ToString("dd MMM yyyy"); } catch { }

                HealthStatus status;
                string label, verdict;
                string? reco = null;

                if (!avEnabled || !amService)         { status = HealthStatus.Critical;  label = "Désactivé";    verdict = "Windows Defender est désactivé. Ton PC n'est pas protégé."; reco = "Activer Windows Defender ou un autre antivirus."; }
                else if (!rtEnabled)                   { status = HealthStatus.Warning;   label = "RT off";        verdict = "Protection en temps réel désactivée."; reco = "Activer 'Real-time protection' dans Windows Security."; }
                else if (sigAge > 7)                   { status = HealthStatus.Warning;   label = "Signatures";    verdict = $"Signatures virus vieilles de {sigAge} jours."; reco = "Lancer Windows Update."; }
                else                                    { status = HealthStatus.Excellent; label = "Actif";         verdict = "Defender actif avec protection en temps réel et signatures à jour."; }

                return new ComponentHealth
                {
                    Name = "Windows Defender",
                    Icon = "🛡",
                    Subtitle = "Antivirus / Sécurité Windows",
                    Status = status,
                    StatusLabel = label,
                    Verdict = verdict,
                    Recommendation = reco,
                    Metrics = new()
                    {
                        ("Antivirus",        avEnabled ? "✓ Activé" : "✕ Désactivé"),
                        ("Temps réel",       rtEnabled ? "✓ Activé" : "✕ Désactivé"),
                        ("Âge signatures",   $"{sigAge} jour(s)"),
                        ("Dernière analyse", lastScan)
                    }
                };
            }
        }
        catch { }
        return FallbackUnknown("Windows Defender", "🛡", "Antivirus / Sécurité");
    }

    private static ComponentHealth AnalyzeNetwork()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            if (interfaces.Count == 0)
            {
                return new ComponentHealth
                {
                    Name = "Connexion réseau",
                    Icon = "📡",
                    Subtitle = "Wi-Fi / Ethernet",
                    Status = HealthStatus.Critical,
                    StatusLabel = "Hors ligne",
                    Verdict = "Aucune interface réseau active.",
                    Recommendation = "Vérifier câble ou Wi-Fi.",
                    Metrics = new() { ("Interfaces actives", "0") }
                };
            }

            var primary = interfaces.OrderByDescending(n => n.Speed).First();
            var speedMbps = primary.Speed / 1_000_000;
            var ip = primary.GetIPProperties().UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString() ?? "--";
            var typeLabel = primary.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Ethernet => "Ethernet",
                NetworkInterfaceType.Wireless80211 => "Wi-Fi",
                _ => primary.NetworkInterfaceType.ToString()
            };

            HealthStatus status;
            string label, verdict;
            if (speedMbps >= 100) { status = HealthStatus.Excellent; label = "Connecté"; verdict = $"Connexion {typeLabel} à {speedMbps} Mbps."; }
            else if (speedMbps >= 10) { status = HealthStatus.Good; label = "Connecté"; verdict = $"Connexion {typeLabel} à {speedMbps} Mbps."; }
            else { status = HealthStatus.Warning; label = "Lent"; verdict = $"Vitesse de lien faible: {speedMbps} Mbps."; }

            return new ComponentHealth
            {
                Name = "Connexion réseau",
                Icon = "📡",
                Subtitle = typeLabel + " — " + primary.Name,
                Status = status,
                StatusLabel = label,
                Verdict = verdict,
                Metrics = new()
                {
                    ("Type",            typeLabel),
                    ("Vitesse lien",    $"{speedMbps} Mbps"),
                    ("Adresse IP",      ip),
                    ("Interfaces UP",   interfaces.Count.ToString())
                }
            };
        }
        catch
        {
            return FallbackUnknown("Connexion réseau", "📡", "Wi-Fi / Ethernet");
        }
    }

    private static ComponentHealth AnalyzeThermalEvents()
    {
        try
        {
            var query = new EventLogQuery("System", PathType.LogName,
                "*[System[Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=88 or EventID=120) and TimeCreated[timediff(@SystemTime) <= 604800000]]]");
            using var reader = new EventLogReader(query);

            var events = new List<(DateTime time, string msg)>();
            EventRecord? rec;
            while ((rec = reader.ReadEvent()) != null)
            {
                try
                {
                    if (rec.TimeCreated.HasValue)
                    {
                        var msg = rec.FormatDescription() ?? "Événement thermique";
                        events.Add((rec.TimeCreated.Value, msg.Split('\n')[0]));
                    }
                }
                finally { rec.Dispose(); }
            }

            var count24h = events.Count(e => (DateTime.Now - e.time).TotalHours <= 24);
            var count7d = events.Count;
            var lastEvent = events.OrderByDescending(e => e.time).FirstOrDefault();

            HealthStatus status;
            string label, verdict;
            string? reco = null;

            if (count24h >= 3)     { status = HealthStatus.Critical;  label = "Critique";  verdict = $"{count24h} hibernations thermiques dans les dernières 24h!"; reco = "Nettoyer ventilateurs immédiatement, remplacer pâte thermique."; }
            else if (count24h >= 1) { status = HealthStatus.Warning;   label = "Récents";   verdict = $"{count24h} hibernation(s) thermique(s) dans les dernières 24h."; reco = "Surveiller températures, prévoir maintenance du cooling."; }
            else if (count7d >= 3)  { status = HealthStatus.Warning;   label = "Présents";  verdict = $"{count7d} événements thermiques sur 7 jours."; reco = "Vérifier ventilation, nettoyer poussière."; }
            else if (count7d >= 1)  { status = HealthStatus.Good;      label = "Rares";     verdict = $"{count7d} événement(s) sur 7 jours — situation maîtrisée."; }
            else                     { status = HealthStatus.Excellent; label = "Aucun";     verdict = "Aucun événement thermique sur 7 jours. Cooling fonctionne bien."; }

            return new ComponentHealth
            {
                Name = "Événements thermiques",
                Icon = "🔥",
                Subtitle = "Hibernations forcées / overheating",
                Status = status,
                StatusLabel = label,
                Verdict = verdict,
                Recommendation = reco,
                Metrics = new()
                {
                    ("Dernières 24h",  count24h.ToString()),
                    ("7 derniers jours", count7d.ToString()),
                    ("Dernier événement", lastEvent.time != default ? lastEvent.time.ToString("dd MMM HH:mm") : "Aucun"),
                    ("Seuil critique",   "3 par 24h")
                }
            };
        }
        catch
        {
            return FallbackUnknown("Événements thermiques", "🔥", "Hibernations / overheating");
        }
    }
}
