using System.Diagnostics.Eventing.Reader;
using System.Management;
using System.Net.NetworkInformation;
using YassirDiagno.Models;

namespace YassirDiagno.Services;

public enum HealthStatus { Excellent, Good, Warning, Critical, Unknown }

public sealed record HealthMetric(string Key, string Value, HealthStatus Status = HealthStatus.Good);
public sealed record HealthIssue(string Problem, string Cause, string Solution);

public sealed class ComponentHealth
{
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public required string Subtitle { get; init; }
    public required HealthStatus Status { get; init; }
    public required string StatusLabel { get; init; }
    public List<HealthMetric> Metrics { get; init; } = new();
    public required string Verdict { get; init; }
    public string? Recommendation { get; init; }
    public List<HealthIssue> Issues { get; init; } = new();
    public string? Thresholds { get; init; }
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

    private static HealthStatus TempStatus(double t, double warn, double hot, double crit)
        => t >= crit ? HealthStatus.Critical : t >= hot ? HealthStatus.Warning : t >= warn ? HealthStatus.Good : HealthStatus.Excellent;

    private static ComponentHealth AnalyzeCpu(HardwareSnapshot s, HardwareIdentity id)
    {
        var temp = s.CpuSilicon;
        var load = s.CpuTotalLoad;
        var clock = s.CpuAverageClock;
        var power = s.CpuPower;

        HealthStatus status;
        string label, verdict;
        string? reco = null;
        var issues = new List<HealthIssue>();

        if (temp is null) { status = HealthStatus.Unknown; label = "Inconnu"; verdict = "Données capteurs indisponibles."; }
        else if (temp >= 90)   { status = HealthStatus.Critical; label = "Surchauffe"; verdict = $"Température silicon {temp:F1}°C, proche du throttling.";
                                  issues.Add(new("CPU surchauffe critique", $"Température {temp:F1}°C ≥ 90°C", "Vérifier ventilation, nettoyer poussière, refaire pâte thermique")); }
        else if (temp >= 75)   { status = HealthStatus.Warning;  label = "Chaud";      verdict = $"Température silicon {temp:F1}°C élevée.";
                                  issues.Add(new("CPU chaud", $"Température {temp:F1}°C ≥ 75°C", "Surveiller la charge, vérifier ventilation")); }
        else if (temp >= 55)   { status = HealthStatus.Good;     label = "Normal";     verdict = $"Fonctionnement normal à {temp:F1}°C."; }
        else                    { status = HealthStatus.Excellent; label = "Excellent"; verdict = $"Température basse à {temp:F1}°C, refroidissement optimal."; }

        if (load is double ld && ld > 95)
            issues.Add(new("CPU saturé", $"Charge {ld:F0}% ≥ 95%", "Identifier processus gourmand dans page Processus"));

        return new ComponentHealth
        {
            Name = "Processeur",
            Icon = "🖥",
            Subtitle = id.CpuName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Issues = issues,
            Thresholds = "Normal <55°C · Tiède 55-75°C · Chaud 75-90°C · Critique ≥90°C",
            Metrics = new()
            {
                new("Température silicon", temp is double t ? $"{t:F1} °C" : "--"),
                new("Charge totale",       load is double l ? $"{l:F1} %" : "--"),
                new("Fréquence moyenne",   clock is double c ? $"{c/1000:F2} GHz" : "--"),
                new("Puissance",           power is double p ? $"{p:F1} W" : "--")
            }
        };
    }

    private static ComponentHealth AnalyzeRam(HardwareSnapshot s)
    {
        var pct = s.RamUsagePercent;
        HealthStatus status;
        string label, verdict;
        string? reco = null;
        var issues = new List<HealthIssue>();

        if (pct is null)         { status = HealthStatus.Unknown;   label = "Inconnu";       verdict = "Mesure indisponible."; }
        else if (pct >= 90)       { status = HealthStatus.Critical;  label = "Saturée";       verdict = $"RAM à {pct:F0}% — saturation, performances dégradées.";
                                     issues.Add(new("RAM saturée", $"Utilisation {pct:F0}% ≥ 90%", "Fermer apps inutilisées, augmenter RAM, redémarrer")); }
        else if (pct >= 75)       { status = HealthStatus.Warning;   label = "Bien utilisée"; verdict = $"RAM à {pct:F0}%, marge réduite.";
                                     issues.Add(new("RAM élevée", $"Utilisation {pct:F0}% ≥ 75%", "Surveiller, fermer applications de fond")); }
        else if (pct >= 50)       { status = HealthStatus.Good;      label = "Normale";       verdict = $"RAM à {pct:F0}%, marge confortable."; }
        else                       { status = HealthStatus.Excellent; label = "Disponible";    verdict = $"RAM à {pct:F0}%, beaucoup de mémoire libre."; }

        return new ComponentHealth
        {
            Name = "Mémoire RAM",
            Icon = "💾",
            Subtitle = "Mémoire vive du système",
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Issues = issues,
            Thresholds = "Libre <50% · Normale 50-75% · Élevée 75-90% · Saturée ≥90%",
            Metrics = new()
            {
                new("Utilisation",     pct is double p ? $"{p:F0} %" : "--"),
                new("Utilisée",        s.RamUsedGB is double u ? $"{u:F1} GB" : "--"),
                new("Total installée", s.RamTotalGB is double t ? $"{t:F1} GB" : "--"),
                new("Disponible",      (s.RamTotalGB - s.RamUsedGB) is double f ? $"{f:F1} GB" : "--")
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
        var issues = new List<HealthIssue>();

        var worstScore = 100.0;
        if (temp is double t)
        {
            if (t >= 78) { worstScore = Math.Min(worstScore, 20); issues.Add(new("SSD surchauffe", $"Température {t:F1}°C ≥ 78°C", "Améliorer ventilation, ajouter dissipateur SSD")); }
            else if (t >= 65) { worstScore = Math.Min(worstScore, 50); issues.Add(new("SSD chaud", $"Température {t:F1}°C", "Surveiller charge I/O, vérifier ventilation")); }
            else if (t >= 50) worstScore = Math.Min(worstScore, 75);
        }
        if (life is double l)
        {
            if (l < 50) { worstScore = Math.Min(worstScore, 20); issues.Add(new("SSD en fin de vie", $"Vie restante {l:F0}% < 50%", "Sauvegarder données, prévoir remplacement")); }
            else if (l < 70) { worstScore = Math.Min(worstScore, 50); issues.Add(new("SSD usure notable", $"Vie restante {l:F0}%", "Surveiller, limiter écritures massives")); }
            else if (l < 85) worstScore = Math.Min(worstScore, 75);
        }

        if (worstScore >= 90)      { status = HealthStatus.Excellent; label = "Excellent"; verdict = "SSD en parfaite santé, températures fraîches."; }
        else if (worstScore >= 70) { status = HealthStatus.Good;      label = "Bon";       verdict = "SSD fonctionne normalement."; }
        else if (worstScore >= 40) { status = HealthStatus.Warning;   label = "Surveiller"; verdict = "Usure ou température notable."; }
        else                        { status = HealthStatus.Critical;  label = "Critique";  verdict = "SSD en fin de vie ou surchauffe critique."; }

        return new ComponentHealth
        {
            Name = "Stockage SSD",
            Icon = "💿",
            Subtitle = id.SsdName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Issues = issues,
            Thresholds = "Temp normal <50°C · Warn 65°C · Critique ≥78°C · Vie minimum 85%",
            Metrics = new()
            {
                new("Température",       temp is double t1 ? $"{t1:F1} °C" : "--"),
                new("Vie restante",      life is double lr ? $"{lr:F0} %" : "--"),
                new("Spare disponible",  spare is double sp ? $"{sp:F0} %" : "--"),
                new("Seuil critique",    "78 °C (vendor)")
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
        var issues = new List<HealthIssue>();

        if (health is null) { status = HealthStatus.Unknown; label = "Inconnue"; verdict = "Données batterie indisponibles."; }
        else if (health < 50) { status = HealthStatus.Critical; label = "À remplacer"; verdict = $"Santé {health:F0}% — autonomie sérieusement réduite.";
                                 issues.Add(new("Batterie épuisée", $"Santé {health:F0}% < 50%", "Remplacement batterie recommandé")); }
        else if (health < 70) { status = HealthStatus.Warning;  label = "Dégradée";    verdict = $"Santé {health:F0}% — usure notable.";
                                 issues.Add(new("Batterie usée", $"Santé {health:F0}% < 70%", "Surveiller, prévoir remplacement")); }
        else if (health < 85) { status = HealthStatus.Good;     label = "Correcte";    verdict = $"Santé {health:F0}% — légère usure normale."; }
        else                   { status = HealthStatus.Excellent;label = "Excellente";  verdict = $"Santé {health:F0}% — proche du neuf."; }

        if (charge is double c && c < 15 && statusStr != "charging" && statusStr != "on AC")
            issues.Add(new("Batterie faible", $"Charge {c:F0}% < 15%", "Brancher chargeur"));

        return new ComponentHealth
        {
            Name = "Batterie",
            Icon = "🔋",
            Subtitle = id.BatteryName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Issues = issues,
            Thresholds = "Excellente >85% · Correcte 70-85% · Dégradée 50-70% · Remplacer <50%",
            Metrics = new()
            {
                new("Charge actuelle", charge is double cc ? $"{cc:F0} %" : "--"),
                new("Santé",           health is double h ? $"{h:F0} %" : "--"),
                new("Statut",          statusStr ?? "--"),
                new("Capacité perdue", health is double hh ? $"~{100-hh:F0} %" : "--")
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
        var issues = new List<HealthIssue>();

        if (load is null) { status = HealthStatus.Unknown; label = "Inconnu"; verdict = "Données GPU indisponibles."; }
        else if (load >= 90 || (temp ?? 0) >= 85) { status = HealthStatus.Critical; label = "Saturé"; verdict = "GPU à pleine charge ou très chaud.";
                                                     issues.Add(new("GPU saturé", $"Charge {load:F0}% / Temp {temp:F0}°C", "Réduire la charge graphique, fermer jeux/3D")); }
        else if (load >= 70 || (temp ?? 0) >= 70) { status = HealthStatus.Warning;  label = "Élevé";  verdict = "Activité graphique intense."; }
        else if (load >= 30)                       { status = HealthStatus.Good;     label = "Modéré"; verdict = "Activité graphique normale."; }
        else                                        { status = HealthStatus.Excellent; label = "Idle";  verdict = "GPU au repos."; }

        var memUsed = s.GpuMemoryUsedMB;
        var memTotal = s.GpuMemoryTotalMB;

        return new ComponentHealth
        {
            Name = "Carte graphique",
            Icon = "🎮",
            Subtitle = id.GpuName,
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Issues = issues,
            Thresholds = "Idle <30% · Modéré 30-70% · Élevé 70-90% · Saturé ≥90%",
            Metrics = new()
            {
                new("Charge",        load is double l ? $"{l:F0} %" : "--"),
                new("Température",   temp is double t ? $"{t:F0} °C" : "--"),
                new("VRAM utilisée", memUsed is double mu2 ? $"{mu2:F0} MB" : "--"),
                new("VRAM totale",   memTotal is double mt2 ? $"{mt2:F0} MB" : "--")
            }
        };
    }

    private static ComponentHealth AnalyzeDiskSpace(HardwareSnapshot s)
    {
        var freePct = s.DiskFreePercent;
        HealthStatus status;
        string label, verdict;
        string? reco = null;
        var issues = new List<HealthIssue>();

        if (freePct is null)        { status = HealthStatus.Unknown;   label = "Inconnu";   verdict = "Données disque indisponibles."; }
        else if (freePct < 7)        { status = HealthStatus.Critical;  label = "Critique";  verdict = $"Disque à {freePct:F1}% libre — Windows peut dysfonctionner.";
                                        issues.Add(new("Disque presque plein", $"Espace libre {freePct:F1}% < 7%", "Libérer 5+ GB urgent: Disk Cleanup, désinstaller apps, vider Téléchargements")); }
        else if (freePct < 15)       { status = HealthStatus.Warning;   label = "Attention"; verdict = $"Disque à {freePct:F1}% libre — faible.";
                                        issues.Add(new("Espace faible", $"Espace libre {freePct:F1}% < 15%", "Libérer espace: vider corbeille, supprimer fichiers temporaires")); }
        else if (freePct < 30)       { status = HealthStatus.Good;      label = "Correct";   verdict = $"Disque à {freePct:F1}% libre — suffisant."; }
        else                          { status = HealthStatus.Excellent; label = "Excellent"; verdict = $"Disque à {freePct:F1}% libre — beaucoup d'espace."; }

        return new ComponentHealth
        {
            Name = "Espace disque",
            Icon = "📂",
            Subtitle = "Lecteur système (C:)",
            Status = status,
            StatusLabel = label,
            Verdict = verdict,
            Recommendation = reco,
            Issues = issues,
            Thresholds = "Excellent >30% · Correct 15-30% · Attention 7-15% · Critique <7%",
            Metrics = new()
            {
                new("Libre",   s.DiskFreeGB is double f ? $"{f:F1} GB" : "--"),
                new("Total",   s.DiskTotalGB is double t ? $"{t:F1} GB" : "--"),
                new("Utilisé", (s.DiskTotalGB - s.DiskFreeGB) is double u ? $"{u:F1} GB" : "--"),
                new("Libre %", freePct is double p ? $"{p:F1} %" : "--")
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
                var issues = new List<HealthIssue>();

                if (!avEnabled || !amService) { status = HealthStatus.Critical; label = "Désactivé"; verdict = "Windows Defender est désactivé.";
                                                 issues.Add(new("Antivirus désactivé", "Defender AV/service off", "Réactiver Defender via Windows Security")); }
                else if (!rtEnabled)           { status = HealthStatus.Warning;  label = "RT off";   verdict = "Protection en temps réel désactivée.";
                                                 issues.Add(new("Protection temps réel off", "Real-time protection désactivée", "Activer 'Real-time protection' dans Windows Security")); }
                else if (sigAge > 7)           { status = HealthStatus.Warning;  label = "Signatures"; verdict = $"Signatures virus vieilles de {sigAge} jours.";
                                                 issues.Add(new("Signatures obsolètes", $"Âge {sigAge}j > 7j", "Lancer Windows Update")); }
                else                            { status = HealthStatus.Excellent; label = "Actif";    verdict = "Defender actif et à jour."; }

                return new ComponentHealth
                {
                    Name = "Windows Defender",
                    Icon = "🛡",
                    Subtitle = "Antivirus / Sécurité Windows",
                    Status = status,
                    StatusLabel = label,
                    Verdict = verdict,
                    Issues = issues,
                    Thresholds = "Signatures <7 jours · RT activée · Service actif",
                    Metrics = new()
                    {
                        new("Antivirus",        avEnabled ? "✓ Activé" : "✕ Désactivé"),
                        new("Temps réel",       rtEnabled ? "✓ Activé" : "✕ Désactivé"),
                        new("Âge signatures",   $"{sigAge} jour(s)"),
                        new("Dernière analyse", lastScan)
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
                    Issues = new() { new("Pas de réseau", "Aucune interface UP", "Vérifier câble Ethernet ou activer Wi-Fi") },
                    Metrics = new() { new("Interfaces actives", "0") }
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
            var issues = new List<HealthIssue>();
            if (speedMbps >= 100) { status = HealthStatus.Excellent; label = "Connecté"; verdict = $"Connexion {typeLabel} à {speedMbps} Mbps."; }
            else if (speedMbps >= 10) { status = HealthStatus.Good; label = "Connecté"; verdict = $"Connexion {typeLabel} à {speedMbps} Mbps."; }
            else { status = HealthStatus.Warning; label = "Lent"; verdict = $"Vitesse de lien faible: {speedMbps} Mbps.";
                    issues.Add(new("Lien lent", $"{speedMbps} Mbps", "Vérifier qualité câble / proximité routeur")); }

            return new ComponentHealth
            {
                Name = "Connexion réseau",
                Icon = "📡",
                Subtitle = typeLabel + " — " + primary.Name,
                Status = status,
                StatusLabel = label,
                Verdict = verdict,
                Issues = issues,
                Thresholds = "Excellent ≥100 Mbps · Bon 10-100 Mbps · Lent <10 Mbps",
                Metrics = new()
                {
                    new("Type",          typeLabel),
                    new("Vitesse lien",  $"{speedMbps} Mbps"),
                    new("Adresse IP",    ip),
                    new("Interfaces UP", interfaces.Count.ToString())
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

            var events = new List<DateTime>();
            EventRecord? rec;
            while ((rec = reader.ReadEvent()) != null)
            {
                try { if (rec.TimeCreated.HasValue) events.Add(rec.TimeCreated.Value); }
                finally { rec.Dispose(); }
            }

            var count24h = events.Count(e => (DateTime.Now - e).TotalHours <= 24);
            var count7d = events.Count;
            var lastEvent = events.OrderByDescending(e => e).FirstOrDefault();

            HealthStatus status;
            string label, verdict;
            var issues = new List<HealthIssue>();

            if (count24h >= 3) { status = HealthStatus.Critical; label = "Critique"; verdict = $"{count24h} hibernations thermiques en 24h!";
                                  issues.Add(new("Surchauffe répétée", $"{count24h} hibernations thermiques en 24h", "Maintenance urgente: nettoyer ventilateurs, refaire pâte thermique")); }
            else if (count24h >= 1) { status = HealthStatus.Warning; label = "Récents"; verdict = $"{count24h} hibernation(s) en 24h.";
                                       issues.Add(new("Événement thermique récent", $"{count24h} en 24h, {count7d} sur 7j", "Surveiller températures, planifier maintenance du cooling")); }
            else if (count7d >= 3) { status = HealthStatus.Warning; label = "Présents"; verdict = $"{count7d} événements sur 7 jours.";
                                     issues.Add(new("Historique thermique chargé", $"{count7d} événements sur 7j", "Vérifier ventilation, nettoyer poussière préventivement")); }
            else if (count7d >= 1) { status = HealthStatus.Good; label = "Rares"; verdict = $"{count7d} événement(s) sur 7 jours — situation maîtrisée."; }
            else { status = HealthStatus.Excellent; label = "Aucun"; verdict = "Aucun événement sur 7 jours. Cooling OK."; }

            return new ComponentHealth
            {
                Name = "Événements thermiques",
                Icon = "🔥",
                Subtitle = "Hibernations forcées / overheating",
                Status = status,
                StatusLabel = label,
                Verdict = verdict,
                Issues = issues,
                Thresholds = "Aucun = excellent · ≥1 en 24h = warning · ≥3 en 24h = critique",
                Metrics = new()
                {
                    new("Dernières 24h",     count24h.ToString()),
                    new("7 derniers jours",  count7d.ToString()),
                    new("Dernier événement", lastEvent != default ? lastEvent.ToString("dd MMM HH:mm") : "Aucun"),
                    new("Seuil critique",    "3 par 24h")
                }
            };
        }
        catch
        {
            return FallbackUnknown("Événements thermiques", "🔥", "Hibernations / overheating");
        }
    }
}
